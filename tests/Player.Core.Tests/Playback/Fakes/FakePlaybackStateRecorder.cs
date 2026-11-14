using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Tests.Playback.Fakes;

internal sealed class FakePlaybackStateRecorder : IPlaybackStateRecorder
{
    private int _flushCount;

    public Exception? FlushException { get; set; }
    public int FlushCount => Volatile.Read(ref _flushCount);

    public void Update(Func<PlaybackState, PlaybackState> change)
    {
    }

    public Task FlushAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _flushCount);
        return FlushException is { } ex ? Task.FromException(ex) : Task.CompletedTask;
    }
}
