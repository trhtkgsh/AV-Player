using Player.Core.Contracts;
using System.Diagnostics;

namespace Player.App;

public partial class App : Application
{
	private readonly IThemeService _themeService;

	public App(IThemeService themeService)
	{
		ArgumentNullException.ThrowIfNull(themeService);
		_themeService = themeService;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Runs synchronously up to its first real await, so the tokens exist before the first page is created.
		InitializeTheme();
		return new Window(new AppShell());
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
}
