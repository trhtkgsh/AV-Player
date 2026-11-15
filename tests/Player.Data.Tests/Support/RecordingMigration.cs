using Player.Core.Contracts;

namespace Player.Data.Tests.Support;

/// <summary>Hand-written migration that records when it ran and can run extra work or throw.</summary>
public sealed class RecordingMigration : IDbMigration
{
    private readonly List<int> _log;
    private readonly Func<IDbConnectionProvider, Task>? _body;
    private int _runCount;

    public RecordingMigration(int order, List<int> log, Func<IDbConnectionProvider, Task>? body = null)
    {
        Order = order;
        _log = log;
        _body = body;
    }

    public int Order { get; }

    public int RunCount => Volatile.Read(ref _runCount);

    public async Task ApplyAsync(IDbConnectionProvider db, CancellationToken ct)
    {
        Interlocked.Increment(ref _runCount);
        lock (_log)
        {
            _log.Add(Order);
        }

        if (_body is not null)
        {
            await _body(db);
        }
    }
}
