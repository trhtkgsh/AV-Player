using Android.Content;
using Android.Graphics;
using Android.Media.Session;
using Android.OS;
using Android.Views;
using Player.Core.Contracts;
using Player.Core.Logic.Session;
using Player.Core.Models;
using AndroidMetadata = global::Android.Media.MediaMetadata;
using AndroidPlaybackState = global::Android.Media.Session.PlaybackState;
using AndroidStateCode = global::Android.Media.Session.PlaybackStateCode;
using Log = global::Android.Util.Log;

namespace Player.App.MediaControls;

/// <summary>
/// The one MediaSession of the process (spec 09). It is created on the main thread the first time it is needed and is
/// never released. It is active while the coordinator has a current item. Its state follows the engine's snapshots
/// through <see cref="SessionStateMapper"/>, its commands go to the contracts on the thread pool.
/// No notification, no audio focus, no Bluetooth API: the service owns the first two, the system owns the rest.
/// </summary>
public sealed class MediaSessionBridge : IMediaSessionBridge
{
    internal const string Tag = "ArielSession";
    private const string SessionTag = "ArielPlayer";
    private const int DefaultSkipSeconds = 10;
    private const int MaxArtworkPixels = 1024;

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IMediaMetadataService _metadata;
    private readonly IUiSettingsStore _uiSettings;
    private readonly MultiPressDetector _buttons;
    private readonly SessionPublishGate _gate;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private readonly object _publishLock = new();
    private MediaSession? _session;
    private bool _active;
    private int _started;
    private QueueAvailability _availability = QueueAvailability.None;
    private MediaItem? _metaItem;
    private long _metaDurationMs;
    private Bitmap? _artwork;

    public MediaSessionBridge(
        IPlayerEngine engine,
        IPlaybackCoordinator coordinator,
        IMediaMetadataService metadata,
        IUiSettingsStore uiSettings,
        TimeProvider time)
    {
        _engine = engine;
        _coordinator = coordinator;
        _metadata = metadata;
        _uiSettings = uiSettings;
        _gate = new SessionPublishGate(time);
        _buttons = new MultiPressDetector(time, OnButtonDecision);
    }

    public object? SessionToken => Volatile.Read(ref _session)?.SessionToken;

    /// <summary>Starts following the engine and the coordinator. Idempotent. Creates nothing until an item is open.</summary>
    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        _engine.SnapshotChanged += OnSnapshotChanged;
        _coordinator.Changed += OnCoordinatorChanged;
        if (_coordinator.Current is not null)
        {
            ScheduleSync();
        }
    }

    public async Task ActivateAsync(CancellationToken ct)
    {
        await _syncGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await ActivateCoreAsync().ConfigureAwait(false);
            SyncItem();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public async Task DeactivateAsync(CancellationToken ct)
    {
        await _syncGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            DeactivateCore();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Engine thread. Only the snapshot argument is used: nothing here calls back into the engine.
    private void OnSnapshotChanged(object? sender, PlayerSnapshot snapshot)
    {
        try
        {
            PublishState(snapshot, force: false);
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Publishing the state failed: {ex}");
        }
    }

    private void OnCoordinatorChanged(object? sender, EventArgs e) => ScheduleSync();

    private void ScheduleSync() => _ = Task.Run(SyncAsync);

    private async Task SyncAsync()
    {
        try
        {
            await _syncGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_coordinator.Current is null)
                {
                    DeactivateCore();
                }
                else
                {
                    await ActivateCoreAsync().ConfigureAwait(false);
                    SyncItem();
                }
            }
            finally
            {
                _syncGate.Release();
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Following the coordinator failed: {ex}");
        }
    }

    // Makes sure the session exists and is active. The session is created on the main thread, once.
    private async Task ActivateCoreAsync()
    {
        if (_session is not null && _active)
        {
            return;
        }

        await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
        {
            var session = _session;
            if (session is null)
            {
                session = new MediaSession(global::Android.App.Application.Context, SessionTag);
                session.SetCallback(new SessionCallback(this), new Handler(Looper.MainLooper!));
                Volatile.Write(ref _session, session);
                Log.Debug(Tag, "Media session created");
            }

            session.Active = true;
            lock (_publishLock)
            {
                _active = true;
            }
        }).ConfigureAwait(false);
        Log.Debug(Tag, "Media session active");
    }

    private void DeactivateCore()
    {
        lock (_publishLock)
        {
            var session = _session;
            if (session is null || !_active)
            {
                return;
            }

            var oldArtwork = _artwork;
            _metaItem = null;
            _artwork = null;
            _availability = QueueAvailability.None;
            _gate.Reset();
            session.SetPlaybackState(BuildState(SessionState.Empty));
            session.SetMetadata(null);
            session.Active = false;
            _active = false;
            ReleaseArtwork(oldArtwork);
            Log.Debug(Tag, "Media session inactive");
        }
    }

    // After an item change or an activation: queue info, metadata and one forced state.
    private void SyncItem()
    {
        var item = _coordinator.Current;
        if (item is null)
        {
            return;
        }

        var changed = false;
        lock (_publishLock)
        {
            _availability = SessionStateMapper.GetQueueAvailability(_coordinator.Queue, item, _coordinator.Repeat);
            if (_metaItem?.Id != item.Id)
            {
                changed = true;
                var oldArtwork = _artwork;
                _metaItem = item;
                _metaDurationMs = 0;
                _artwork = null;
                _gate.Reset();
                PublishMetadataLocked();
                ReleaseArtwork(oldArtwork);
            }

            PublishState(_engine.Snapshot, force: true);
        }

        if (changed)
        {
            _ = Task.Run(() => LoadCachedMetadataAsync(item));
        }
    }

    // Cached rows only: no decoding of the media, only of the small cached JPEG, off the main thread.
    private async Task LoadCachedMetadataAsync(MediaItem item)
    {
        try
        {
            var rows = await _metadata.GetCachedAsync(new[] { item.Id }, CancellationToken.None).ConfigureAwait(false);
            if (!rows.TryGetValue(item.Id, out var row))
            {
                return;
            }

            var artwork = row.ThumbnailPath is { Length: > 0 } path && File.Exists(path) ? LoadArtwork(path) : null;
            lock (_publishLock)
            {
                if (_metaItem?.Id != item.Id || _session is null)
                {
                    ReleaseArtwork(artwork); // Never published: nobody else holds it.
                    return;
                }

                var oldArtwork = _artwork;
                _artwork = artwork;
                if (_metaDurationMs <= 0)
                {
                    _metaDurationMs = row.DurationMs;
                }

                PublishMetadataLocked();
                ReleaseArtwork(oldArtwork);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Loading the cached metadata failed: {ex.Message}");
        }
    }

    // Called only after the metadata that replaces the bitmap was set (SetMetadata is a synchronous call and the system
    // keeps its own copy), so the session no longer reads it.
    private static void ReleaseArtwork(Bitmap? bitmap)
    {
        if (bitmap is null)
        {
            return;
        }

        try
        {
            bitmap.Recycle();
            bitmap.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Releasing the artwork failed: {ex.Message}");
        }
    }

    private static Bitmap? LoadArtwork(string path)
    {
        var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeFile(path, bounds);
        var sample = 1;
        for (var side = Math.Max(bounds.OutWidth, bounds.OutHeight); side / sample > MaxArtworkPixels; sample *= 2)
        {
        }

        return BitmapFactory.DecodeFile(path, new BitmapFactory.Options { InSampleSize = sample });
    }

    private void PublishMetadataLocked()
    {
        var session = _session;
        var item = _metaItem;
        if (session is null || item is null)
        {
            return;
        }

        var builder = new AndroidMetadata.Builder()
            .PutString(AndroidMetadata.MetadataKeyMediaId, item.Id.ToKey())!
            .PutString(AndroidMetadata.MetadataKeyTitle, System.IO.Path.GetFileNameWithoutExtension(item.DisplayName))!
            .PutString(AndroidMetadata.MetadataKeyAlbum, item.FolderName)!;
        if (_metaDurationMs > 0)
        {
            builder.PutLong(AndroidMetadata.MetadataKeyDuration, _metaDurationMs);
        }

        if (_artwork is { } art)
        {
            builder.PutBitmap(AndroidMetadata.MetadataKeyArt, art);
            builder.PutBitmap(AndroidMetadata.MetadataKeyAlbumArt, art);
        }

        session.SetMetadata(builder.Build());
        Log.Debug(Tag, $"Metadata published: {item.DisplayName}, {_metaDurationMs} ms, artwork {_artwork is not null}");
    }

    private void PublishState(PlayerSnapshot snapshot, bool force)
    {
        lock (_publishLock)
        {
            var session = _session;
            if (session is null || !_active)
            {
                return;
            }

            var current = _coordinator.Current;
            if (current is not null && snapshot.Current != current.Id)
            {
                // The engine has not caught up with the item change yet.
                snapshot = snapshot with { Status = PlayerStatus.Opening, Current = current.Id, PositionMs = 0, DurationMs = 0 };
            }

            if (current is not null && _metaItem?.Id == current.Id && snapshot.DurationMs > 0
                && snapshot.DurationMs != _metaDurationMs)
            {
                _metaDurationMs = snapshot.DurationMs;
                PublishMetadataLocked();
            }

            var state = SessionStateMapper.Map(snapshot, current is not null, _availability);
            if (!_gate.ShouldPublish(state) && !force)
            {
                return;
            }

            session.SetPlaybackState(BuildState(state));
            Log.Debug(Tag, $"State published: {state.State} {state.PositionMs} ms x{state.Speed} actions={state.Actions}");
        }
    }

    private static AndroidPlaybackState BuildState(SessionState state)
    {
        var code = state.State switch
        {
            SessionPlaybackState.Playing => AndroidStateCode.Playing,
            SessionPlaybackState.Paused => AndroidStateCode.Paused,
            SessionPlaybackState.Buffering => AndroidStateCode.Buffering,
            SessionPlaybackState.Error => AndroidStateCode.Error,
            SessionPlaybackState.Stopped => AndroidStateCode.Stopped,
            _ => AndroidStateCode.None,
        };

        var builder = new AndroidPlaybackState.Builder()
            .SetActions(ToActions(state.Actions))!
            .SetState(code, state.PositionMs, state.Speed)!;
        if (state.Error is not null)
        {
            builder.SetErrorMessage(state.Error);
        }

        return builder.Build()!;
    }

    private static long ToActions(SessionActions actions)
    {
        long result = 0;
        Add(SessionActions.Play, AndroidPlaybackState.ActionPlay);
        Add(SessionActions.Pause, AndroidPlaybackState.ActionPause);
        Add(SessionActions.PlayPause, AndroidPlaybackState.ActionPlayPause);
        Add(SessionActions.Stop, AndroidPlaybackState.ActionStop);
        Add(SessionActions.SeekTo, AndroidPlaybackState.ActionSeekTo);
        Add(SessionActions.FastForward, AndroidPlaybackState.ActionFastForward);
        Add(SessionActions.Rewind, AndroidPlaybackState.ActionRewind);
        Add(SessionActions.SkipToNext, AndroidPlaybackState.ActionSkipToNext);
        Add(SessionActions.SkipToPrevious, AndroidPlaybackState.ActionSkipToPrevious);
        return result;

        void Add(SessionActions flag, long platformAction)
        {
            if (actions.HasFlag(flag))
            {
                result |= platformAction;
            }
        }
    }

    // Commands. Each one leaves the callback thread at once and never throws.
    internal void Play() => Run("play", ct => _engine.PlayAsync(ct));

    internal void Pause() => Run("pause", ct => _engine.PauseAsync(ct));

    internal void TogglePlayPause() => Run("play/pause", ct =>
        _engine.Snapshot.Status == PlayerStatus.Playing ? _engine.PauseAsync(ct) : _engine.PlayAsync(ct));

    internal void Stop() => Run("stop", ct => _coordinator.CloseAsync(ct));

    internal void Next() => Run("next", ct => _coordinator.NextAsync(ct));

    internal void Previous() => Run("previous", ct => _coordinator.PreviousAsync(ct));

    internal void SeekTo(long positionMs) => Run("seek", ct => _engine.SeekAsync(positionMs, ct));

    /// <param name="direction">1 fast forward, -1 rewind.</param>
    internal void Skip(int direction) => Run(direction > 0 ? "fast forward" : "rewind", async ct =>
    {
        var seconds = (await _uiSettings.GetAsync(ct).ConfigureAwait(false)).SkipSeconds;
        await _coordinator.SkipAsync(direction * (seconds > 0 ? seconds : DefaultSkipSeconds) * 1000L, ct)
            .ConfigureAwait(false);
    });

    internal void OnHeadsetButton() => _buttons.Press();

    private void OnButtonDecision(MediaButtonAction action)
    {
        switch (action)
        {
            case MediaButtonAction.TogglePlayPause:
                TogglePlayPause();
                break;
            case MediaButtonAction.Next:
                Next();
                break;
            default:
                Previous();
                break;
        }
    }

    private static void Run(string name, Func<CancellationToken, Task> command)
    {
        Log.Debug(Tag, $"Command: {name}");
        _ = Task.Run(async () =>
        {
            try
            {
                await command(CancellationToken.None).ConfigureAwait(false);
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log.Warn(Tag, $"Command {name} failed: {ex}");
            }
        });
    }

    private sealed class SessionCallback(MediaSessionBridge bridge) : MediaSession.Callback
    {
        public override void OnPlay() => bridge.Play();

        public override void OnPause() => bridge.Pause();

        public override void OnStop() => bridge.Stop();

        public override void OnSkipToNext() => bridge.Next();

        public override void OnSkipToPrevious() => bridge.Previous();

        public override void OnSeekTo(long pos) => bridge.SeekTo(pos);

        public override void OnFastForward() => bridge.Skip(1);

        public override void OnRewind() => bridge.Skip(-1);

        // The headset button (and the play/pause key of some remotes) is grouped into 1, 2 and 3 presses. Every other
        // key goes to the base class, which calls the methods above.
        public override bool OnMediaButtonEvent(Intent mediaButtonIntent)
        {
            var keyEvent = ReadKeyEvent(mediaButtonIntent);
            if (keyEvent is { KeyCode: Keycode.Headsethook or Keycode.MediaPlayPause })
            {
                if (keyEvent.Action == KeyEventActions.Down && keyEvent.RepeatCount == 0)
                {
                    bridge.OnHeadsetButton();
                }

                return true;
            }

            return base.OnMediaButtonEvent(mediaButtonIntent);
        }

        private static KeyEvent? ReadKeyEvent(Intent intent)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return intent.GetParcelableExtra(Intent.ExtraKeyEvent, Java.Lang.Class.FromType(typeof(KeyEvent))) as KeyEvent;
            }

#pragma warning disable CA1422 // The typed overload exists only from API 33.
            return intent.GetParcelableExtra(Intent.ExtraKeyEvent) as KeyEvent;
#pragma warning restore CA1422
        }
    }
}
