// Phase 1 placeholder. Replaced, not extended, in Phase 2.
using System.Collections.Concurrent;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Composition.Placeholders;

public sealed class InMemoryPlaybackStateStore : IPlaybackStateStore
{
	private readonly ConcurrentDictionary<MediaFileId, PlaybackState> _states = new();

	public Task<PlaybackState?> GetAsync(MediaFileId id, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		return Task.FromResult<PlaybackState?>(_states.TryGetValue(id, out var state) ? state : null);
	}

	public Task<IReadOnlyDictionary<MediaFileId, PlaybackState>> GetManyAsync(
		IReadOnlyCollection<MediaFileId> ids, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(ids);
		ct.ThrowIfCancellationRequested();

		var found = new Dictionary<MediaFileId, PlaybackState>();
		foreach (var id in ids)
		{
			if (_states.TryGetValue(id, out var state))
			{
				found[id] = state;
			}
		}

		return Task.FromResult<IReadOnlyDictionary<MediaFileId, PlaybackState>>(found);
	}

	public Task SaveAsync(PlaybackState state, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(state);
		ct.ThrowIfCancellationRequested();
		_states[state.Id] = state;
		return Task.CompletedTask;
	}

	public Task DeleteAsync(MediaFileId id, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		_states.TryRemove(id, out _);
		return Task.CompletedTask;
	}
}
