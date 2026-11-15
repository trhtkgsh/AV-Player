using CommunityToolkit.Mvvm.ComponentModel;

namespace Player.App.Features.PlayerHud;

/// <summary>
/// The on-screen indicator of the player: a short text that hides by itself, or stays while a gesture is held.
/// Every member is called on the UI thread.
/// </summary>
public sealed partial class OsdViewModel : ObservableObject
{
    private const int ShowMs = 900;
    private const int ReleaseMs = 400;

    private CancellationTokenSource? _hideCts;

    [ObservableProperty] private string _text = "";
    [ObservableProperty] private bool _isVisible;

    /// <summary>Shows a text and hides it 900 ms after the last call. Never throws.</summary>
    public void Show(string text)
    {
        Present(text);
        ScheduleHide(ShowMs);
    }

    /// <summary>Shows a text without a timeout, until <see cref="Release"/> or <see cref="Hide"/>.</summary>
    public void Hold(string text) => Present(text);

    /// <summary>Hides the held text after 400 ms.</summary>
    public void Release() => ScheduleHide(ReleaseMs);

    /// <summary>Hides the text at once and cancels a pending hide.</summary>
    public void Hide()
    {
        CancelPending();
        IsVisible = false;
    }

    private void Present(string text)
    {
        CancelPending();
        Text = text;
        IsVisible = true;
    }

    private void ScheduleHide(int delayMs)
    {
        CancelPending();
        var cts = new CancellationTokenSource();
        _hideCts = cts;
        _ = HideAfterAsync(delayMs, cts.Token);
    }

    private void CancelPending()
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = null;
    }

    private async Task HideAfterAsync(int delayMs, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delayMs, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // The continuation runs on the UI thread. A Show, Hold or Hide that ran after the delay completed cancelled ct.
        if (!ct.IsCancellationRequested) IsVisible = false;
    }
}
