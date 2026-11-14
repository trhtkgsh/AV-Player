// Phase 1 placeholder. Replaced in Phase 4 by the media service.
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Player.Core.Contracts;

namespace Player.App.Composition.Placeholders;

public sealed class PauseOnBackgroundPlaceholder : IMauiInitializeService
{
	private IPlayerEngine? _engine;

	public void Initialize(IServiceProvider services)
	{
		ArgumentNullException.ThrowIfNull(services);

		_engine = services.GetRequiredService<IPlayerEngine>();
		var lifecycle = services.GetRequiredService<IAppLifecycle>();
		lifecycle.Backgrounded += OnBackgrounded;
	}

	private void OnBackgrounded(object? sender, EventArgs e) => PauseEngineAsync();

	private async void PauseEngineAsync()
	{
		try
		{
			if (_engine is null)
			{
				return;
			}

			await _engine.PauseAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Pause on background failed: {ex}");
		}
	}
}
