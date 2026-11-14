using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Tests.Playback.Fakes;

internal sealed class FakePlaybackStateStore : IPlaybackStateStore
{
    private int _getCount;

    public Dictionary<MediaFileId, PlaybackState> States { get; } = new();
    public Exception? GetException { get; set; }
    public int GetCount => Volatile.Read(ref _getCount);

    public Task<PlaybackState?> GetAsync(MediaFileId id, CancellationToken ct)
    {
        Interlocked.Increment(ref _getCount);
        if (GetException is { } ex)
        {
            return Task.FromException<PlaybackState?>(ex);
        }

        return Task.FromResult(States.TryGetValue(id, out var state) ? state : null);
    }

    public Task<IReadOnlyDictionary<MediaFileId, PlaybackState>> GetManyAsync(
        IReadOnlyCollection<MediaFileId> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<MediaFileId, PlaybackState>>(new Dictionary<MediaFileId, PlaybackState>());

    public Task SaveAsync(PlaybackState state, CancellationToken ct) => Task.CompletedTask;

    public Task DeleteAsync(MediaFileId id, CancellationToken ct) => Task.CompletedTask;
}
