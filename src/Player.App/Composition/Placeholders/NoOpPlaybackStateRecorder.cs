// Phase 1 placeholder. Replaced, not extended, in Phase 2.
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Composition.Placeholders;

public sealed class NoOpPlaybackStateRecorder : IPlaybackStateRecorder
{
	public void Update(Func<PlaybackState, PlaybackState> change)
	{
	}

	public Task FlushAsync(CancellationToken ct) => Task.CompletedTask;
}
