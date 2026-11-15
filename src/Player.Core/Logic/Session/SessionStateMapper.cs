using Player.Core.Models;

namespace Player.Core.Logic.Session;

/// <summary>The playback state a media session shows, without any platform type.</summary>
public enum SessionPlaybackState { None, Stopped, Paused, Playing, Buffering, Error }

/// <summary>The transport actions a media session offers.</summary>
[Flags]
public enum SessionActions
{
    None = 0,
    Play = 1,
    Pause = 2,
    PlayPause = 4,
    Stop = 8,
    SeekTo = 16,
    FastForward = 32,
    Rewind = 64,
    SkipToNext = 128,
    SkipToPrevious = 256,
}

/// <summary>What a media session publishes about playback.</summary>
/// <param name="PositionMs">0 .. duration (when the duration is known).</param>
/// <param name="Speed">The playback speed while playing, 0 in every other state, so remote displays can extrapolate.</param>
/// <param name="Error">The engine's message in the Error state, otherwise null.</param>
public sealed record SessionState(
    SessionPlaybackState State,
    long PositionMs,
    float Speed,
    SessionActions Actions,
    string? Error)
{
    public static SessionState Empty { get; } = new(SessionPlaybackState.None, 0, 0f, SessionActions.None, null);
}

/// <summary>Whether the coordinator can go to another item of its queue.</summary>
public readonly record struct QueueAvailability(bool HasNext, bool HasPrevious)
{
    public static QueueAvailability None => new(false, false);
}

/// <summary>Turns the engine's snapshot and the queue into the state of the media session. Pure.</summary>
public static class SessionStateMapper
{
    private const SessionActions OpenItemActions =
        SessionActions.Play | SessionActions.Pause | SessionActions.PlayPause | SessionActions.Stop
        | SessionActions.SeekTo | SessionActions.FastForward | SessionActions.Rewind;

    /// <param name="hasItem">True while the coordinator has a current item. Without one nothing is offered.</param>
    public static SessionState Map(PlayerSnapshot snapshot, bool hasItem, QueueAvailability queue)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!hasItem)
        {
            return SessionState.Empty;
        }

        var state = snapshot.Status switch
        {
            PlayerStatus.Playing => SessionPlaybackState.Playing,
            PlayerStatus.Paused => SessionPlaybackState.Paused,
            PlayerStatus.Opening => SessionPlaybackState.Buffering,
            PlayerStatus.Error => SessionPlaybackState.Error,
            _ => SessionPlaybackState.Stopped,
        };

        var position = Math.Max(0, snapshot.PositionMs);
        if (snapshot.DurationMs > 0)
        {
            position = Math.Min(position, snapshot.DurationMs);
        }

        var actions = OpenItemActions;
        if (queue.HasNext)
        {
            actions |= SessionActions.SkipToNext;
        }

        if (queue.HasPrevious)
        {
            actions |= SessionActions.SkipToPrevious;
        }

        var speed = state == SessionPlaybackState.Playing ? snapshot.Speed : 0f;
        var error = state == SessionPlaybackState.Error ? snapshot.Error ?? string.Empty : null;
        return new SessionState(state, position, speed, actions, error);
    }

    /// <summary>
    /// Next and previous as the coordinator offers them. The coordinator does not expose its index, so it is found
    /// by id. Folder repeat wraps around the ends. When the current item is not in the queue, both are offered
    /// whenever the queue has more than one item.
    /// </summary>
    public static QueueAvailability GetQueueAvailability(
        IReadOnlyList<MediaItem> queue, MediaItem? current, RepeatMode repeat)
    {
        ArgumentNullException.ThrowIfNull(queue);
        if (current is null || queue.Count < 2)
        {
            return QueueAvailability.None;
        }

        var index = -1;
        for (var i = 0; i < queue.Count; i++)
        {
            if (queue[i].Id == current.Id)
            {
                index = i;
                break;
            }
        }

        if (index < 0 || repeat == RepeatMode.Folder)
        {
            return new QueueAvailability(true, true);
        }

        return new QueueAvailability(index < queue.Count - 1, index > 0);
    }
}

/// <summary>
/// Decides when a new <see cref="SessionState"/> is worth publishing: when the state, the speed or the actions
/// change, or when the position jumps (a seek) by more than <see cref="JumpToleranceMs"/> against the value
/// extrapolated from the last publication. Plain position ticks are not published. Not thread safe.
/// </summary>
public sealed class SessionPublishGate(TimeProvider time)
{
    public const long JumpToleranceMs = 1500;

    private SessionState? _last;
    private long _lastStamp;

    /// <summary>True when <paramref name="next"/> must be published. The gate then remembers it as published.</summary>
    public bool ShouldPublish(SessionState next)
    {
        ArgumentNullException.ThrowIfNull(next);
        var last = _last;
        var now = time.GetTimestamp();
        var publish = last is null
            || last.State != next.State
            || last.Actions != next.Actions
            || last.Speed != next.Speed
            || !string.Equals(last.Error, next.Error, StringComparison.Ordinal)
            || Math.Abs(next.PositionMs - Extrapolate(last, now)) > JumpToleranceMs;
        if (publish)
        {
            _last = next;
            _lastStamp = now;
        }

        return publish;
    }

    /// <summary>Forgets the last publication, so the next state is published (after an item change).</summary>
    public void Reset() => _last = null;

    private long Extrapolate(SessionState last, long now)
    {
        var elapsedMs = time.GetElapsedTime(_lastStamp, now).TotalMilliseconds;
        return last.PositionMs + (long)(elapsedMs * last.Speed);
    }
}
