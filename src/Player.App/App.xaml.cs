using Player.Core.Contracts;
using System.Diagnostics;

namespace Player.App;

public partial class App : Application
{
	private readonly IThemeService _themeService;
	private readonly Player.App.Localization.Localizer _localizer;

	public App(IThemeService themeService, Player.App.Localization.Localizer localizer)
	{
		ArgumentNullException.ThrowIfNull(themeService);
		ArgumentNullException.ThrowIfNull(localizer);
		_themeService = themeService;
		_localizer = localizer;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Runs synchronously up to its first real await, so the tokens exist before the first page is created.
		InitializeTheme();
		InitializeLocalization();
		var window = new Window(new AppShell());
		_localizer.ApplyFlowDirection(window);
		return window;
	}

	private async void InitializeTheme()
	{
		try
		{
			await _themeService.InitializeAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Theme initialization failed: {ex}");
		}
	}

	private async void InitializeLocalization()
	{
		try
		{
			await _localizer.InitializeAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Localization initialization failed: {ex}");
		}
	}
}
