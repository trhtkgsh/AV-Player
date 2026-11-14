// Phase 1 placeholder. Replaced, not extended, in Phase 2.
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Composition.Placeholders;

public sealed class InMemoryUiSettingsStore : IUiSettingsStore
{
	private readonly object _gate = new();
	private UiSettings _value = new();

	public event EventHandler<UiSettings>? Changed;

	public Task<UiSettings> GetAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		lock (_gate)
		{
			return Task.FromResult(_value);
		}
	}

	public Task SaveAsync(UiSettings value, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(value);
		ct.ThrowIfCancellationRequested();
		lock (_gate)
		{
			_value = value;
		}

		Changed?.Invoke(this, value);
		return Task.CompletedTask;
	}
}
