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
/// State and commands of the subtitle sheet. Observes the engine; never owns or disposes it.
/// Writes the track and the delay only to the current file's state, never to the global defaults.
/// </summary>
public sealed partial class SubtitleSheetViewModel : ObservableObject
{
    private const int OffTrackId = -1;
    private const long DelayStepMs = 50;
    private const long MinDelayMs = -5000;
    private const long MaxDelayMs = 5000;

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly ISubtitleService _subtitles;
    private readonly IPlaybackStateRecorder _recorder;
    private bool _active;
    private bool _closing;
    private bool _navigating;

    [ObservableProperty] private long _delayMs;
    [ObservableProperty] private string _delayText = "0 ms";
    [ObservableProperty] private bool _hasFiles;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _errorMessage = "";

    /// <summary>"Off" first, then the subtitle tracks of the file. The check mark follows the engine.</summary>
    public ObservableCollection<TrackRow> Tracks { get; } = new();

    /// <summary>Subtitle files found next to the video.</summary>
    public ObservableCollection<SubtitleFileRow> Files { get; } = new();

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public SubtitleSheetViewModel(
        IPlayerEngine engine,
        IPlaybackCoordinator coordinator,
        ISubtitleService subtitles,
        IPlaybackStateRecorder recorder)
    {
        _engine = engine;
        _coordinator = coordinator;
        _subtitles = subtitles;
        _recorder = recorder;
    }

    /// <summary>Called when the sheet appears. Shows the state in effect, starts the file list, and subscribes.</summary>
    public void Activate()
    {
        if (_active) return;

        _active = true;
        _engine.TracksChanged += OnTracksChanged;
        ErrorMessage = "";
        DelayMs = _engine.SubtitleDelayMs;
        RefreshTracks();
        _ = LoadFilesAsync();
    }

    /// <summary>Called when the sheet disappears. Unsubscribes. Safe to call more than once.</summary>
    public void Deactivate()
    {
        _active = false;
        _engine.TracksChanged -= OnTracksChanged;
    }

    partial void OnDelayMsChanged(long value) => DelayText = FormatDelay(value);

    /// <summary>Selects the track of the row, then records it for this file. "Off" is the row with id -1.</summary>
    [RelayCommand]
    private async Task SelectTrackAsync(TrackRow row)
    {
        try
        {
            await _engine.SelectSubtitleTrackAsync(row.Id, CancellationToken.None);
            _recorder.Update(state => state with { SubtitleTrackId = row.Id });
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            RefreshTracks();
        }
    }

    /// <summary>Loads a file from the list next to the video.</summary>
    [RelayCommand]
    private async Task LoadFileAsync(SubtitleFileRow row)
    {
        try
        {
            bool ok = await _subtitles.LoadExternalAsync(row.ContentUri, CancellationToken.None);
            ErrorMessage = ok ? "" : "The subtitle file could not be loaded.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Opens the system file picker. A cancelled pick is not an error.</summary>
    [RelayCommand]
    private async Task PickFileAsync()
    {
        try
        {
            bool ok = await _subtitles.PickAndLoadAsync(CancellationToken.None);
            if (ok) ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // AllowConcurrentExecutions: each tap changes DelayMs at once, so a quick second tap is not lost.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task DelayDownAsync() => ApplyDelayAsync(DelayMs - DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task DelayUpAsync() => ApplyDelayAsync(DelayMs + DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task DelayResetAsync() => ApplyDelayAsync(0);

    /// <summary>Opens the subtitle style sheet: this sheet closes first. A second tap while navigating is ignored.</summary>
    [RelayCommand]
    private async Task OpenStyleAsync()
    {
        if (_navigating) return;
        _navigating = true;

        try
        {
            if (Shell.Current is { } shell)
            {
                await shell.GoToAsync("..");
                await shell.GoToAsync("player-subtitle-style");
            }
        }
        catch (Exception ex)
        {
            _navigating = false; // navigation failed: allow a later tap to try again
            Debug.WriteLine($"Open subtitle style failed: {ex}");
        }
    }

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
            await _engine.SetSubtitleDelayAsync(value, CancellationToken.None);
            _recorder.Update(state => state with { SubtitleDelayMs = value });
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            DelayMs = _engine.SubtitleDelayMs;
        }
    }

    /// <summary>
    /// Fills the file list once per activation. Runs without blocking the sheet; a failure leaves the list empty.
    /// </summary>
    private async Task LoadFilesAsync()
    {
        var rows = new List<SubtitleFileRow>();

        try
        {
            if (_coordinator.Current is { } item)
            {
                IReadOnlyList<SubtitleFile> found = await _subtitles.FindSidecarsAsync(item, CancellationToken.None);
                foreach (SubtitleFile file in found)
                {
                    rows.Add(new SubtitleFileRow(file.ContentUri, file.DisplayName));
                }
            }
        }
        catch (Exception ex)
        {
            rows.Clear();
            Debug.WriteLine($"Finding subtitle files failed: {ex}");
        }

        Files.Clear();
        foreach (SubtitleFileRow row in rows)
        {
            Files.Add(row);
        }

        HasFiles = Files.Count > 0;
    }

    // The engine raises TracksChanged on its own thread: marshal before touching properties.
    private void OnTracksChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() => { if (_active) RefreshTracks(); });

    /// <summary>
    /// Rebuilds the rows only when the ids or titles changed. Otherwise only the check marks are updated,
    /// so the list does not flicker while the user taps.
    /// </summary>
    private void RefreshTracks()
    {
        IReadOnlyList<TrackInfo> tracks = _engine.SubtitleTracks;
        if (!SameTracks(tracks))
        {
            Tracks.Clear();
            Tracks.Add(new TrackRow(OffTrackId, "Off", isSelected: false));
            foreach (TrackInfo track in tracks)
            {
                Tracks.Add(new TrackRow(track.Id, TrackRow.TitleOf(track), isSelected: false));
            }
        }

        int? current = _engine.CurrentSubtitleTrackId;
        foreach (TrackRow row in Tracks)
        {
            row.IsSelected = current is { } id && row.Id == id;
        }
    }

    private bool SameTracks(IReadOnlyList<TrackInfo> tracks)
    {
        if (tracks.Count + 1 != Tracks.Count) return false;
        if (Tracks[0].Id != OffTrackId) return false;

        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i].Id != Tracks[i + 1].Id || TrackRow.TitleOf(tracks[i]) != Tracks[i + 1].Title) return false;
        }

        return true;
    }

    private static string FormatDelay(long value) =>
        value.ToString("+0;-0;0", CultureInfo.InvariantCulture) + " ms";
}
