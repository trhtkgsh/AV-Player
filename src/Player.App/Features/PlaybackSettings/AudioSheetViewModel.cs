using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlaybackSettings;

/// <summary>
/// State and commands of the audio sheet. Observes the engine; never owns or disposes it.
/// Writes the track and the delay only to the current file's state, never to the global defaults.
/// </summary>
public sealed partial class AudioSheetViewModel : ObservableObject
{
    private const long DelayStepMs = 50;
    private const long MinDelayMs = -5000;
    private const long MaxDelayMs = 5000;

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackStateRecorder _recorder;
    private bool _active;
    private bool _closing;

    [ObservableProperty] private long _delayMs;
    [ObservableProperty] private string _delayText = "0 ms";
    [ObservableProperty] private bool _hasNoTracks = true;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _errorMessage = "";

    /// <summary>The audio tracks of the file. The check mark follows the engine's selected track.</summary>
    public ObservableCollection<TrackRow> Tracks { get; } = new();

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public AudioSheetViewModel(IPlayerEngine engine, IPlaybackStateRecorder recorder)
    {
        _engine = engine;
        _recorder = recorder;
    }

    /// <summary>Called when the sheet appears. Shows the track and the delay in effect, and subscribes.</summary>
    public void Activate()
    {
        if (_active) return;

        _active = true;
        _engine.TracksChanged += OnTracksChanged;
        ErrorMessage = "";
        DelayMs = _engine.AudioDelayMs;
        RefreshTracks();
    }

    /// <summary>Called when the sheet disappears. Unsubscribes. Safe to call more than once.</summary>
    public void Deactivate()
    {
        _active = false;
        _engine.TracksChanged -= OnTracksChanged;
    }

    partial void OnDelayMsChanged(long value) => DelayText = FormatDelay(value);

    /// <summary>Selects the track of the row, then records it for this file.</summary>
    [RelayCommand]
    private async Task SelectTrackAsync(TrackRow row)
    {
        try
        {
            await _engine.SelectAudioTrackAsync(row.Id, CancellationToken.None);
            _recorder.Update(state => state with { AudioTrackId = row.Id });
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ResetFromEngine();
        }
    }

    // AllowConcurrentExecutions: each tap changes DelayMs at once, so a quick second tap is not lost.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task DelayDownAsync() => ApplyDelayAsync(DelayMs - DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task DelayUpAsync() => ApplyDelayAsync(DelayMs + DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task DelayResetAsync() => ApplyDelayAsync(0);

    /// <summary>Closes the sheet and returns to the player. Runs once while a close is in progress.</summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        if (_closing) return;
        _closing = true;

        try
        {
            if (Shell.Current is { } shell) await shell.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _closing = false; // close failed: allow a later tap or Back press to try again
            Debug.WriteLine($"Close failed: {ex}");
        }
    }

    private async Task ApplyDelayAsync(long requested)
    {
        long value = Math.Clamp(requested, MinDelayMs, MaxDelayMs);
        DelayMs = value; // synchronous: the next tap steps from this value

        try
        {
            await _engine.SetAudioDelayAsync(value, CancellationToken.None);
            _recorder.Update(state => state with { AudioDelayMs = value });
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ResetFromEngine();
        }
    }

    // The engine raises TracksChanged on its own thread: marshal before touching properties.
    private void OnTracksChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() => { if (_active) RefreshTracks(); });

    /// <summary>Puts the delay and the track check back to the values the engine really has.</summary>
    private void ResetFromEngine()
    {
        DelayMs = _engine.AudioDelayMs;
        RefreshTracks();
    }

    /// <summary>
    /// Rebuilds the rows only when the ids or titles changed. Otherwise only the check marks are updated,
    /// so the list does not flicker while the user taps.
    /// </summary>
    private void RefreshTracks()
    {
        IReadOnlyList<TrackInfo> tracks = _engine.AudioTracks;
        if (!SameTracks(tracks))
        {
            Tracks.Clear();
            foreach (TrackInfo track in tracks)
            {
                Tracks.Add(new TrackRow(track.Id, TrackRow.TitleOf(track), isSelected: false));
            }
        }

        int? current = _engine.CurrentAudioTrackId;
        foreach (TrackRow row in Tracks)
        {
            row.IsSelected = row.Id == current;
        }

        HasNoTracks = Tracks.Count == 0;
    }

    private bool SameTracks(IReadOnlyList<TrackInfo> tracks)
    {
        if (tracks.Count != Tracks.Count) return false;

        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i].Id != Tracks[i].Id || TrackRow.TitleOf(tracks[i]) != Tracks[i].Title) return false;
        }

        return true;
    }

    private static string FormatDelay(long value) =>
        value.ToString("+0;-0;0", CultureInfo.InvariantCulture) + " ms";
}
