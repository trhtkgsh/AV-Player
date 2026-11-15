using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Player.Core.Contracts;
using Player.Core.Models;
using AndroidApp = Android.App.Application;
using AndroidLog = Android.Util.Log;

namespace Player.App.Background;

/// <summary>
/// Decides when the playback service runs and what happens to playback around it: the service starts
/// when a file plays and stops when playback is closed or has been paused in the background for ten
/// minutes; audio focus; pause when the sound would move to the speaker; background audio or pause
/// when the app leaves the screen. It creates no engine and no session: it follows the app's singletons.
/// Every field is touched on the UI thread only, except where marked.
/// </summary>
internal sealed class PlaybackServiceController : IPlaybackServiceController
{
    internal const string Tag = "ArielService";
    private const int NotificationId = 1;
    private static readonly TimeSpan IdleStop = TimeSpan.FromMinutes(10);

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IMediaSessionBridge _session;
    private readonly IGlobalDefaultsStore _defaults;
    private readonly IAppLifecycle _lifecycle;
    private readonly IPlaybackStateRecorder _recorder;
    private readonly ILocalizer _localizer;
    private readonly PlaybackNotification _notification;
    private readonly Handler _handler = new(Looper.MainLooper!);

    private readonly object _seenLock = new();
    private PlayerStatus _seenStatus = PlayerStatus.Idle;   // under _seenLock
    private MediaFileId? _seenItem;                         // under _seenLock
    private int _started;                                   // Interlocked
    private volatile bool _backgroundAudio;                 // written by the defaults store's thread
    private volatile bool _running;

    private PlaybackService? _service;
    private bool _starting;
    private bool _videoOff;          // this class switched the picture off for background audio
    private bool _hasFocus;
    private bool _resumeOnGain;      // playback was paused by a short loss of the audio focus
    private AudioFocusRequestClass? _focusRequest;
    private NoisyReceiver? _noisy;
    private bool _idleArmed;
    private int _idleToken;

    public PlaybackServiceController(
        IPlayerEngine engine,
        IPlaybackCoordinator coordinator,
        IMediaSessionBridge session,
        IGlobalDefaultsStore defaults,
        IAppLifecycle lifecycle,
        IPlaybackStateRecorder recorder,
        ILocalizer localizer)
    {
        _engine = engine;
        _coordinator = coordinator;
        _session = session;
        _defaults = defaults;
        _lifecycle = lifecycle;
        _recorder = recorder;
        _localizer = localizer;
        _notification = new PlaybackNotification(localizer);
    }

    public bool IsRunning => _running;

    public Task StartAsync(CancellationToken ct)
    {
        OnUiThread(EnsureService);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        OnUiThread(() =>
        {
            StopService();
            AbandonFocus();
        });
        return Task.CompletedTask;
    }

    /// <summary>Called once at app start.</summary>
    internal void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
            return;

        _engine.SnapshotChanged += OnSnapshotChanged;
        _coordinator.Changed += (_, _) => OnUiThread(Evaluate);
        _lifecycle.Backgrounded += (_, _) => OnUiThread(OnBackgrounded);
        _lifecycle.Foregrounded += (_, _) => OnUiThread(OnForegrounded);
        _defaults.Changed += (_, value) => _backgroundAudio = value.BackgroundAudio;
        _localizer.Changed += (_, _) => OnUiThread(RefreshNotification);
        Fire(async ct => _backgroundAudio = (await _defaults.GetAsync(ct).ConfigureAwait(false)).BackgroundAudio);
    }

    // ---- service ------------------------------------------------------------------------------

    /// <summary>The service was started, or one of the notification's buttons was pressed.</summary>
    internal void OnServiceCommand(PlaybackService service, string? action)
    {
        _starting = false;
        if (!ReferenceEquals(_service, service))
        {
            _service = service;
            _running = true;

            // Android requires the notification within five seconds of the start, wanted or not.
            EnterForeground(service);
            RegisterNoisyReceiver();
        }

        if (_coordinator.Current is null)
        {
            StopService();
            return;
        }

        switch (action)
        {
            case PlaybackNotification.ActionToggle:
                if (_engine.Snapshot.Status == PlayerStatus.Playing)
                    Fire(_engine.PauseAsync);
                else
                    Fire(_engine.PlayAsync);
                break;
            case PlaybackNotification.ActionNext:
                Fire(_coordinator.NextAsync);
                break;
            case PlaybackNotification.ActionPrevious:
                Fire(_coordinator.PreviousAsync);
                break;
            case PlaybackNotification.ActionClose:
                ClosePlayback();
                break;
        }
    }

    internal void OnServiceDestroyed(PlaybackService service)
    {
        if (!ReferenceEquals(_service, service))
            return;

        _service = null;
        _running = false;
        UnregisterNoisyReceiver();
    }

    /// <summary>The app was removed from the recent apps list: playback ends with it.</summary>
    internal void OnTaskRemoved()
    {
        ClosePlayback();
        StopService();
        AbandonFocus();
    }

    private void ClosePlayback() => Fire(async ct =>
    {
        await _recorder.FlushAsync(ct).ConfigureAwait(false);
        await _coordinator.CloseAsync(ct).ConfigureAwait(false);
    });

    private void EnsureService()
    {
        if (_service is not null || _starting || _coordinator.Current is null)
            return;

        _starting = true;
        Fire(async ct =>
        {
            try
            {
                // The notification is tied to the media session: the token must exist first.
                await _session.ActivateAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AndroidLog.Warn(Tag, $"The media session could not be activated: {ex}");
            }

            OnUiThread(StartService);
        });
    }

    private void StartService()
    {
        if (_service is not null || _coordinator.Current is null)
        {
            _starting = false;
            return;
        }

        try
        {
            var context = AndroidApp.Context;
            context.StartForegroundService(new Intent(context, typeof(PlaybackService)));
        }
        catch (Exception ex)
        {
            // From Android 12 a service may not start while the app is in the background (a play
            // command from a headset after the service had stopped). Playback goes on without it.
            _starting = false;
            AndroidLog.Warn(Tag, $"The playback service could not start: {ex}");
        }
    }

    private void EnterForeground(PlaybackService service)
    {
        try
        {
            var notification = BuildNotification(service);
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                service.StartForeground(NotificationId, notification, ForegroundService.TypeMediaPlayback);
            else
                service.StartForeground(NotificationId, notification);
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(Tag, $"The service could not enter the foreground: {ex}");
        }
    }

    private void StopService()
    {
        CancelIdleStop();
        if (_service is not { } service)
            return;

        _service = null;
        _running = false;
        UnregisterNoisyReceiver();
        try
        {
            service.StopForeground(Android.App.StopForegroundFlags.Remove);
            service.StopSelf();
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(Tag, $"Stopping the service failed: {ex}");
        }
    }

    private Android.App.Notification BuildNotification(Context context) =>
        _notification.Build(
            context, _coordinator.Current, _engine.Snapshot.Status == PlayerStatus.Playing, _session.SessionToken);

    private void RefreshNotification()
    {
        if (_service is not { } service)
            return;

        try
        {
            (service.GetSystemService(Context.NotificationService) as Android.App.NotificationManager)
                ?.Notify(NotificationId, BuildNotification(service));
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(Tag, $"The notification could not be updated: {ex}");
        }
    }

    // ---- playback state -----------------------------------------------------------------------

    /// <summary>Engine thread. Only a change of the status or of the file is passed on, not every position.</summary>
    private void OnSnapshotChanged(object? sender, PlayerSnapshot snapshot)
    {
        lock (_seenLock)
        {
            if (snapshot.Status == _seenStatus && Equals(snapshot.Current, _seenItem))
                return;

            _seenStatus = snapshot.Status;
            _seenItem = snapshot.Current;
        }

        OnUiThread(Evaluate);
    }

    private void Evaluate()
    {
        var status = _coordinator.Current is null ? PlayerStatus.Idle : _engine.Snapshot.Status;
        switch (status)
        {
            case PlayerStatus.Playing:
                CancelIdleStop();
                _resumeOnGain = false;
                if (!RequestFocus())
                {
                    // Another app holds the audio and does not give it up (a phone call).
                    Fire(_engine.PauseAsync);
                    return;
                }

                EnsureService();
                break;

            case PlayerStatus.Idle:
            case PlayerStatus.Error:
                _videoOff = false;
                StopService();
                AbandonFocus();
                return;

            default:
                if (!_lifecycle.IsForeground)
                    ArmIdleStop();
                break;
        }

        RefreshNotification();
    }

    // ---- app in the background ------------------------------------------------------------------

    private void OnBackgrounded()
    {
        if (_coordinator.Current is null)
            return;

        var status = _engine.Snapshot.Status;
        if (!_backgroundAudio)
        {
            Fire(_engine.PauseAsync);
            ArmIdleStop();
        }
        else if (status is PlayerStatus.Playing or PlayerStatus.Opening)
        {
            // The sound goes on; nothing is decoded for a picture that nobody sees.
            _videoOff = true;
            Fire(ct => _engine.SetVideoEnabledAsync(false, ct));
        }
        else
        {
            ArmIdleStop();
        }

        Fire(_recorder.FlushAsync);
    }

    private void OnForegrounded()
    {
        CancelIdleStop();
        if (!_videoOff)
            return;

        _videoOff = false;
        Fire(ct => _engine.SetVideoEnabledAsync(true, ct));
    }

    /// <summary>Paused for ten minutes with the app in the background: the service is not needed.</summary>
    private void ArmIdleStop()
    {
        if (_service is null || _idleArmed)
            return;

        _idleArmed = true;
        var token = ++_idleToken;
        _handler.PostDelayed(
            () =>
            {
                if (token != _idleToken || !_idleArmed)
                    return;

                _idleArmed = false;
                if (_engine.Snapshot.Status == PlayerStatus.Playing || _lifecycle.IsForeground)
                    return;

                Fire(_recorder.FlushAsync);
                StopService();
                AbandonFocus();
            },
            (long)IdleStop.TotalMilliseconds);
    }

    private void CancelIdleStop()
    {
        _idleArmed = false;
        _idleToken++;
    }

    // ---- audio focus ----------------------------------------------------------------------------

    private bool RequestFocus()
    {
        if (_hasFocus)
            return true;

        if (AndroidApp.Context.GetSystemService(Context.AudioService) is not AudioManager audio)
            return true;

        try
        {
            // "Can duck" is not handled here: from API 26 the system lowers the volume itself.
            _focusRequest ??= new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Media)!
                    .SetContentType(AudioContentType.Movie)!
                    .Build()!)!
                .SetOnAudioFocusChangeListener(new FocusListener(this), _handler)!
                .Build()!;
            _hasFocus = audio.RequestAudioFocus(_focusRequest) == AudioFocusRequest.Granted;
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(Tag, $"The audio focus request failed: {ex}");
            return true;
        }

        return _hasFocus;
    }

    private void AbandonFocus()
    {
        _resumeOnGain = false;
        if (!_hasFocus || _focusRequest is null)
            return;

        _hasFocus = false;
        try
        {
            (AndroidApp.Context.GetSystemService(Context.AudioService) as AudioManager)
                ?.AbandonAudioFocusRequest(_focusRequest);
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(Tag, $"Giving up the audio focus failed: {ex}");
        }
    }

    /// <summary>UI thread (the listener is registered with the main handler).</summary>
    private void OnFocusChanged(AudioFocus change)
    {
        switch (change)
        {
            case AudioFocus.Gain:
                _hasFocus = true;
                if (_resumeOnGain && _coordinator.Current is not null)
                    Fire(_engine.PlayAsync);
                _resumeOnGain = false;
                break;

            case AudioFocus.LossTransient:
                // A short interruption (a notification sound, a call): resume only what was playing.
                _hasFocus = false;
                _resumeOnGain |= _engine.Snapshot.Status == PlayerStatus.Playing;
                Fire(_engine.PauseAsync);
                break;

            case AudioFocus.Loss:
                _hasFocus = false;
                _resumeOnGain = false;
                Fire(_engine.PauseAsync);
                break;
        }
    }

    // ---- headphones unplugged ---------------------------------------------------------------------

    private void RegisterNoisyReceiver()
    {
        if (_noisy is not null)
            return;

        try
        {
            _noisy = new NoisyReceiver(this);
            AndroidX.Core.Content.ContextCompat.RegisterReceiver(
                AndroidApp.Context, _noisy, new IntentFilter(AudioManager.ActionAudioBecomingNoisy),
                AndroidX.Core.Content.ContextCompat.ReceiverNotExported);
        }
        catch (Exception ex)
        {
            _noisy = null;
            AndroidLog.Warn(Tag, $"The headphones receiver could not be registered: {ex}");
        }
    }

    private void UnregisterNoisyReceiver()
    {
        if (_noisy is not { } receiver)
            return;

        _noisy = null;
        try
        {
            AndroidApp.Context.UnregisterReceiver(receiver);
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(Tag, $"The headphones receiver could not be removed: {ex}");
        }
    }

    private void OnBecomingNoisy()
    {
        _resumeOnGain = false;
        Fire(_engine.PauseAsync);
    }

    // ---- helpers ------------------------------------------------------------------------------------

    private static void OnUiThread(Action action)
    {
        if (MainThread.IsMainThread)
            action();
        else
            MainThread.BeginInvokeOnMainThread(action);
    }

    /// <summary>Runs a command of the engine or the coordinator off the UI thread. Never throws.</summary>
    private static void Fire(Func<CancellationToken, Task> command) => _ = Task.Run(async () =>
    {
        try
        {
            await command(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(Tag, $"A playback command failed: {ex}");
        }
    });

    private sealed class FocusListener(PlaybackServiceController owner)
        : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        public void OnAudioFocusChange(AudioFocus focusChange) => owner.OnFocusChanged(focusChange);
    }

    private sealed class NoisyReceiver(PlaybackServiceController owner) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent) => owner.OnBecomingNoisy();
    }
}
