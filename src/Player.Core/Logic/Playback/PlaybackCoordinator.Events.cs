using Player.Core.Models;

namespace Player.Core.Logic.Playback;

// Engine events. They arrive on the engine's thread: only cheap filtering happens there, the work goes through the
// gate. The decision is always taken on the engine's CURRENT snapshot, read under the gate, never on the snapshot
// captured when the event was raised.
public sealed partial class PlaybackCoordinator
{
    private void OnSnapshotChanged(object? sender, PlayerSnapshot snapshot)
    {
        try
        {
            if (IsDisposed)
            {
                return;
            }

            var ended = false;
            var abJump = false;
            lock (_eventLock)
            {
                if (snapshot.Status == PlayerStatus.Ended)
                {
                    ended = !_endedSeen;
                    _endedSeen = true;
                }
                else
                {
                    _endedSeen = false;
                }

                var ab = _abRepeat;
                if (ab is not null)
                {
                    if (snapshot.PositionMs < ab.EndMs || _time.GetElapsedTime(_abJumpStamp) > AbJumpRearm)
                    {
                        _abJumpPending = false;
                    }

                    if (snapshot.PositionMs >= ab.EndMs && snapshot.Status == PlayerStatus.Playing && !_abJumpPending)
                    {
                        _abJumpPending = true;
                        _abJumpStamp = _time.GetTimestamp();
                        abJump = true;
                    }
                }
            }

            if (ended)
            {
                _ = Task.Run(() => HandleSnapshotAsync(isEnd: true));
            }

            if (abJump)
            {
                _ = Task.Run(() => HandleSnapshotAsync(isEnd: false));
            }
        }
        catch (Exception)
        {
            // No exception may escape into the engine's event.
        }
    }

    private async Task HandleSnapshotAsync(bool isEnd)
    {
        var acted = false;
        try
        {
            var seq = BeginRequest(supersede: false);
            await RunGatedAsync(async token =>
            {
                if (IsDisposed)
                {
                    return;
                }

                var current = Current;
                var snapshot = _engine.Snapshot;
                if (current is null || snapshot.Current != current.Id)
                {
                    return;
                }

                if (isEnd)
                {
                    if (snapshot.Status != PlayerStatus.Ended)
                    {
                        return; // Stale: the file was reopened or restarted meanwhile.
                    }

                    acted = true;
                    var ab = AbRepeat;
                    if (ab is not null)
                    {
                        // A range that ends within the engine's end margin never reaches EndMs: loop on Ended.
                        await _engine.SeekAsync(ab.StartMs, token).ConfigureAwait(false);
                        await _engine.PlayAsync(token).ConfigureAwait(false);
                        return;
                    }

                    await EnsureRepeatLoadedAsync(token).ConfigureAwait(false);
                    if (Repeat == RepeatMode.File)
                    {
                        await RestartAsync(token).ConfigureAwait(false);
                    }
                    else
                    {
                        await AdvanceAsync(1, seq, token).ConfigureAwait(false);
                    }

                    return;
                }

                var range = AbRepeat;
                if (range is not null
                    && snapshot.Status == PlayerStatus.Playing
                    && snapshot.PositionMs >= range.EndMs)
                {
                    acted = true;
                    await _engine.SeekAsync(range.StartMs, token).ConfigureAwait(false);
                }
            }, _lifetimeToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Engine events must never fault the engine or crash the process.
            acted = false;
            Rearm(isEnd, failed: true);
            return;
        }

        if (!acted)
        {
            Rearm(isEnd, failed: false);
        }
    }

    // After a failed reaction the next Ended snapshot is handled again. A stale or failed A-B jump releases the
    // pending flag so the next qualifying snapshot can trigger it.
    private void Rearm(bool isEnd, bool failed)
    {
        lock (_eventLock)
        {
            if (isEnd)
            {
                if (failed)
                {
                    _endedSeen = false;
                }
            }
            else
            {
                _abJumpPending = false;
            }
        }
    }
}
