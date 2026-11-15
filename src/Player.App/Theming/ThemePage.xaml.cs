namespace Player.App.Theming;

public partial class ThemePage : ContentPage
{
    private readonly ThemeViewModel _viewModel;

    public ThemePage(ThemeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Attach();
        _viewModel.Refresh();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Detach();
        base.OnDisappearing();
    }
}
