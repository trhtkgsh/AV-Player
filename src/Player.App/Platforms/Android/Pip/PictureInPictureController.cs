using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.Util;
using Microsoft.Maui.ApplicationModel;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Pip;

/// <summary>
/// Picture-in-Picture for the player. Follows the engine from app start, keeps the system's
/// <see cref="PictureInPictureParams"/> current (aspect ratio, play or pause icon, auto-enter) and
/// reports the PiP state. The activity callbacks are in the other part of this class.
/// </summary>
public sealed partial class PictureInPictureController : IPictureInPictureController, IDisposable
{
    private const string Tag = "ArielPip";

    internal const string ActionPrevious = "Player.App.Pip.PREVIOUS";
    internal const string ActionPlay = "Player.App.Pip.PLAY";
    internal const string ActionPause = "Player.App.Pip.PAUSE";
    internal const string ActionNext = "Player.App.Pip.NEXT";

    // The system accepts 1:2.39 .. 2.39:1. The ratio is built with a denominator of 10000 and kept just inside.
    private const double MaxRatio = 2.39;
    private const int RatioScale = 10000;
    private const int RatioLimit = 23899;

    private readonly IPlayerEngine _engine;
    private readonly IVideoOutputBinding _binding;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IGlobalDefaultsStore _defaults;
    private readonly ILocalizer _localizer;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _gate = new();
    // What the system was really given. Set on the main thread after SetPictureInPictureParams, never before.
    private (bool Playing, VideoFormat? Format, bool OnLeave)? _appliedKey;
    private int _updatePending;
    private volatile bool _leaveSetting;
    private volatile bool _paramsApplied;
    private volatile bool _isActive;
    private volatile bool _disposed;
    private bool? _supported;

    public PictureInPictureController(
        IPlayerEngine engine,
        IVideoOutputBinding binding,
        IPlaybackCoordinator coordinator,
        IGlobalDefaultsStore defaults,
        ILocalizer localizer)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

        _engine.SnapshotChanged += OnSnapshotChanged;
        _binding.FormatChanged += OnFormatChanged;
        _defaults.Changed += OnDefaultsChanged;
        _ = Task.Run(LoadSettingAsync);
    }

    /// <inheritdoc />
    public bool IsSupported => _supported ??=
        OperatingSystem.IsAndroidVersionAtLeast(26) &&
        Platform.AppContext.PackageManager?.HasSystemFeature(PackageManager.FeaturePictureInPicture) == true;

    /// <inheritdoc />
    public bool IsActive => _isActive;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>True while the engine plays and a video picture exists.</summary>
    private bool IsVideoPlaying => _engine.Snapshot.Status == PlayerStatus.Playing && _binding.Format is not null;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.SnapshotChanged -= OnSnapshotChanged;
        _binding.FormatChanged -= OnFormatChanged;
        _defaults.Changed -= OnDefaultsChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    /// <inheritdoc />
    public async Task<bool> EnterAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsSupported || !IsVideoPlaying)
        {
            return false;
        }

        try
        {
            return await MainThread.InvokeOnMainThreadAsync(EnterOnMainThread).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Entering Picture-in-Picture failed: {ex}");
            return false;
        }
    }

    private bool EnterOnMainThread()
    {
        var activity = GetActivity();
        if (activity is null)
        {
            return false;
        }

        if (activity.IsInPictureInPictureMode)
        {
            return true;
        }

        return activity.EnterPictureInPictureMode(BuildParams(activity));
    }

    // ---- Following the engine ----

    private void OnSnapshotChanged(object? sender, PlayerSnapshot snapshot) => RequestUpdate();

    private void OnFormatChanged(object? sender, VideoFormat? format) => RequestUpdate();

    private void OnDefaultsChanged(object? sender, GlobalDefaults value)
    {
        try
        {
            _leaveSetting = value.PictureInPictureOnLeave;
            RequestUpdate();
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Reading the setting failed: {ex}");
        }
    }

    private async Task LoadSettingAsync()
    {
        try
        {
            var value = await _defaults.GetAsync(_lifetime.Token).ConfigureAwait(false);
            _leaveSetting = value.PictureInPictureOnLeave;
            RequestUpdate();
        }
        catch (OperationCanceledException)
        {
            // Disposed.
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Reading the setting failed: {ex}");
        }
    }

    /// <summary>Called from any thread. Posts one update to the main thread when the system's params are out of date.</summary>
    private void RequestUpdate()
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            var key = CurrentKey();
            lock (_gate)
            {
                if (_appliedKey == key)
                {
                    return;
                }
            }

            if (Interlocked.Exchange(ref _updatePending, 1) == 0)
            {
                MainThread.BeginInvokeOnMainThread(UpdateParams);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Scheduling the Picture-in-Picture update failed: {ex}");
        }
    }

    private (bool Playing, VideoFormat? Format, bool OnLeave) CurrentKey() =>
        (_engine.Snapshot.Status == PlayerStatus.Playing, _binding.Format, _leaveSetting);

    // ---- Params ----

    /// <summary>Main thread. Forgets what was applied and applies the params again (a new activity, a PiP mode change).</summary>
    private void ApplyParamsAgain()
    {
        lock (_gate)
        {
            _appliedKey = null;
        }

        UpdateParams();
    }

    /// <summary>
    /// Main thread. Hands the current icon, ratio and auto-enter flag to the system. The key is stored only
    /// after the system has the params: with no activity at hand the next event or activity tries again.
    /// </summary>
    private void UpdateParams()
    {
        Interlocked.Exchange(ref _updatePending, 0);
        try
        {
            if (_disposed || !IsSupported)
            {
                return;
            }

            var key = CurrentKey();
            lock (_gate)
            {
                if (_appliedKey == key)
                {
                    return;
                }
            }

            var activity = GetActivity();
            if (activity is null)
            {
                return;
            }

            // With no video there is nothing to show in a window: switch auto-enter off once, then leave the system alone.
            var hasVideo = key.Format is not null;
            if (hasVideo || _paramsApplied)
            {
                _paramsApplied = hasVideo;
                activity.SetPictureInPictureParams(BuildParams(activity));
            }

            lock (_gate)
            {
                _appliedKey = key;
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Updating the Picture-in-Picture params failed: {ex}");
        }
    }

    private PictureInPictureParams BuildParams(Activity activity)
    {
        var builder = new PictureInPictureParams.Builder();
        var format = _binding.Format;
        if (format is not null)
        {
            builder.SetAspectRatio(ToRational(format));
        }

        builder.SetActions(BuildActions(activity, _engine.Snapshot.Status == PlayerStatus.Playing));
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            builder.SetAutoEnterEnabled(_leaveSetting && IsVideoPlaying);
            builder.SetSeamlessResizeEnabled(true);
        }

        return builder.Build()!;
    }

    /// <summary>The display ratio of the picture (the pixel shape applied), clamped to 1:2.39 .. 2.39:1.</summary>
    internal static Rational ToRational(VideoFormat format)
    {
        var sar = format.SarNumerator > 0 && format.SarDenominator > 0
            ? (double)format.SarNumerator / format.SarDenominator
            : 1.0;
        var ratio = Math.Max(1, format.Width) * sar / Math.Max(1, format.Height);
        ratio = Math.Clamp(ratio, 1.0 / MaxRatio, MaxRatio);

        if (ratio >= 1.0)
        {
            return new Rational(Math.Min((int)Math.Round(ratio * RatioScale), RatioLimit), RatioScale);
        }

        return new Rational(RatioScale, Math.Min((int)Math.Round(RatioScale / ratio), RatioLimit));
    }

    private List<RemoteAction> BuildActions(Activity activity, bool playing)
    {
        var previous = _localizer["Pip.Previous"];
        var next = _localizer["Pip.Next"];
        var middle = playing ? _localizer["Pip.Pause"] : _localizer["Pip.Play"];
        return
        [
            CreateAction(activity, 0, ActionPrevious, Android.Resource.Drawable.IcMediaPrevious, previous),
            playing
                ? CreateAction(activity, 1, ActionPause, Android.Resource.Drawable.IcMediaPause, middle)
                : CreateAction(activity, 2, ActionPlay, Android.Resource.Drawable.IcMediaPlay, middle),
            CreateAction(activity, 3, ActionNext, Android.Resource.Drawable.IcMediaNext, next),
        ];
    }

    // The broadcast goes to this app only (explicit package) and the intent is immutable.
    private static RemoteAction CreateAction(Activity activity, int requestCode, string action, int iconResource, string title)
    {
        var intent = new Intent(action).SetPackage(activity.PackageName);
        var pending = PendingIntent.GetBroadcast(
            activity, requestCode, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
        return new RemoteAction(Icon.CreateWithResource(activity, iconResource)!, title, title, pending);
    }

    private void RaiseChanged()
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((EventHandler)handler)(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Warn(Tag, $"A Changed handler failed: {ex}");
            }
        }
    }
}
