using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Player.App.Features.PlayerHud;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlaybackSettings;

/// <summary>
/// State and commands of the repeat sheet. Observes the coordinator and the engine; never owns or disposes them.
/// The repeat mode and the A-B range are written through the coordinator only.
/// </summary>
public sealed partial class RepeatSheetViewModel : ObservableObject
{
    private const long MinLoopLengthMs = 1000;

    private readonly IPlaybackCoordinator _coordinator;
    private readonly IPlayerEngine _engine;
    private readonly AbRepeatDraft _draft;
    private bool _active;
    private bool _closing;

    [ObservableProperty] private bool _isRepeatNone;
    [ObservableProperty] private bool _isRepeatFile;
    [ObservableProperty] private bool _isRepeatFolder;
    [ObservableProperty] private string _abText = "Not set";
    [ObservableProperty] private bool _canSetB;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _errorMessage = "";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public RepeatSheetViewModel(IPlaybackCoordinator coordinator, IPlayerEngine engine, AbRepeatDraft draft)
    {
        _coordinator = coordinator;
        _engine = engine;
        _draft = draft;
    }

    /// <summary>Called when the sheet appears. Shows the repeat mode and the range in effect, and subscribes.</summary>
    public void Activate()
    {
        if (_active) return;

        // A point A that was set on another file is ignored and dropped.
        if (_draft.FileId != _engine.Snapshot.Current) _draft.Clear();

        _active = true;
        _coordinator.Changed += OnCoordinatorChanged;
        ErrorMessage = "";
        RefreshRepeat();
        RefreshAb();
    }

    /// <summary>Called when the sheet disappears. Unsubscribes. Safe to call more than once.</summary>
    public void Deactivate()
    {
        _active = false;
        _coordinator.Changed -= OnCoordinatorChanged;
    }

    [RelayCommand]
    private Task SetRepeatNoneAsync() => ApplyRepeatAsync(RepeatMode.None);

    [RelayCommand]
    private Task SetRepeatFileAsync() => ApplyRepeatAsync(RepeatMode.File);

    [RelayCommand]
    private Task SetRepeatFolderAsync() => ApplyRepeatAsync(RepeatMode.Folder);

    /// <summary>Stores the current position as A. Any range that is set is cleared first.</summary>
    [RelayCommand]
    private async Task SetAAsync()
    {
        try
        {
            await _coordinator.SetAbRepeatAsync(null, CancellationToken.None);
            _draft.StartMs = _engine.Snapshot.PositionMs;
            _draft.FileId = _engine.Snapshot.Current;
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        RefreshAb();
    }

    /// <summary>Sets the range from A to the current position, when that is at least one second after A.</summary>
    [RelayCommand]
    private async Task SetBAsync()
    {
        if (DraftStartForCurrentFile() is not { } start) return;

        long end = _engine.Snapshot.PositionMs;
        if (end < start + MinLoopLengthMs)
        {
            ErrorMessage = "B must be at least 1 second after A.";
            return;
        }

        try
        {
            await _coordinator.SetAbRepeatAsync(new AbRepeat(start, end), CancellationToken.None);
            _draft.Clear();
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        RefreshAb();
    }

    /// <summary>Removes the range and the point A.</summary>
    [RelayCommand]
    private async Task ClearAbAsync()
    {
        _draft.Clear();
        try
        {
            await _coordinator.SetAbRepeatAsync(null, CancellationToken.None);
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        RefreshAb();
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

    private async Task ApplyRepeatAsync(RepeatMode mode)
    {
        try
        {
            await _coordinator.SetRepeatAsync(mode, CancellationToken.None);
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // The coordinator raises Changed on the engine's or its own threads: marshal before touching properties.
    private void OnCoordinatorChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_active) return;
            RefreshRepeat();
            RefreshAb();
        });

    private void RefreshRepeat()
    {
        RepeatMode mode = _coordinator.Repeat;
        IsRepeatNone = mode == RepeatMode.None;
        IsRepeatFile = mode == RepeatMode.File;
        IsRepeatFolder = mode == RepeatMode.Folder;
    }

    private void RefreshAb()
    {
        AbRepeat? range = _coordinator.AbRepeat;
        long? draftStart = DraftStartForCurrentFile();

        if (range is not null)
        {
            AbText = $"A {TimeFormatter.Format(range.StartMs)}  -  B {TimeFormatter.Format(range.EndMs)}";
        }
        else if (draftStart is { } start)
        {
            AbText = $"A {TimeFormatter.Format(start)}  -  B not set";
        }
        else
        {
            AbText = "Not set";
        }

        CanSetB = draftStart is not null;
    }

    /// <summary>The point A, but only when it was set on the file that is playing now.</summary>
    private long? DraftStartForCurrentFile() =>
        _draft.StartMs is { } start && _draft.FileId == _engine.Snapshot.Current ? start : (long?)null;
}
