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
		// Both run at once. The shell waits for them at most 5 seconds, so a stuck initializer cannot keep the
		// splash page on screen forever. A late result still applies when it finishes.
		var themeTask = InitializeThemeAsync();
		var languageTask = InitializeLanguageAsync();
		var initialization = Task.WhenAll(themeTask, languageTask);
		var timeout = Task.Delay(TimeSpan.FromSeconds(5));
		var winner = await Task.WhenAny(initialization, timeout);
		if (winner != initialization)
		{
			Debug.WriteLine("Theme or language initialization did not finish in 5 seconds; the shell is shown anyway.");
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

		try
		{
			_localizer.ApplyFlowDirection(window);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Applying the flow direction after the shell failed: {ex}");
		}

		async Task InitializeThemeAsync()
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

		async Task InitializeLanguageAsync()
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
}
