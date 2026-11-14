// Phase 1 placeholder. Replaced, not extended, in Phase 2.
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Composition.Placeholders;

public sealed class InMemoryGlobalDefaultsStore : IGlobalDefaultsStore
{
	private readonly object _gate = new();
	private GlobalDefaults _value = new();

	public event EventHandler<GlobalDefaults>? Changed;

	public Task<GlobalDefaults> GetAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		lock (_gate)
		{
			return Task.FromResult(_value);
		}
	}

	public Task SaveAsync(GlobalDefaults value, CancellationToken ct)
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
