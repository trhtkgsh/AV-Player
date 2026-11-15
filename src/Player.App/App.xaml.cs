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
		// A plain splash page until the saved theme and language are applied. The shell is created in StartAsync,
		// so the first real page is never built with the default theme or the wrong language.
		var window = new Window(new ContentPage { BackgroundColor = Colors.Black });
		_localizer.ApplyFlowDirection(window);
		StartAsync(window);
		return window;
	}

	private async void StartAsync(Window window)
	{
		try
		{
			await _themeService.InitializeAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Theme initialization failed: {ex}");
		}

		try
		{
			await _localizer.InitializeAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Localization initialization failed: {ex}");
		}

		try
		{
			window.Page = new AppShell();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Creating the app shell failed: {ex}");
			throw;
		}

		_localizer.ApplyFlowDirection(window);
	}
}
