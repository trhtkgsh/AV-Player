using Microsoft.Maui.Controls;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

// The target folder of a move or copy. One tap chooses; Cancel, or leaving the page, gives no folder.
public partial class FolderPickerPage : ContentPage
{
    private readonly TaskCompletionSource<MediaFolder?> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public FolderPickerPage(IReadOnlyList<MediaFolder> choices)
    {
        InitializeComponent();
        List.ItemsSource = choices;
        Empty.IsVisible = choices.Count == 0;
    }

    // Completes with the chosen folder, or null when the page closes without a choice.
    public Task<MediaFolder?> Result => _result.Task;

    private void OnFolderChosen(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is MediaFolder folder)
        {
            _result.TrySetResult(folder);
        }
    }

    private void OnCancelClicked(object? sender, EventArgs e) => _result.TrySetResult(null);

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _result.TrySetResult(null);
    }
}
