namespace Player.Core.Contracts;

/// <summary>The foreground service that keeps playback alive in the background.</summary>
public interface IPlaybackServiceController
{
    bool IsRunning { get; }
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}

public interface IMediaSessionBridge
{
    /// <summary>The platform session token (Android MediaSession.Token), for the service notification.</summary>
    object? SessionToken { get; }

    Task ActivateAsync(CancellationToken ct);
    Task DeactivateAsync(CancellationToken ct);
}

public interface IPictureInPictureController
{
    bool IsSupported { get; }
    bool IsActive { get; }
    event EventHandler? Changed;

    /// <summary>False when not supported or when no video is playing.</summary>
    Task<bool> EnterAsync(CancellationToken ct);
}

/// <summary>App lifecycle, raised from the main activity.</summary>
public interface IAppLifecycle
{
    bool IsForeground { get; }
    event EventHandler? Foregrounded;
    event EventHandler? Backgrounded;

    /// <summary>Last chance to flush state before the process may be killed.</summary>
    event EventHandler? Stopping;
}

/// <summary>One schema step. Each store ships its own. Applied once, in <see cref="Order"/>.</summary>
public interface IDbMigration
{
    int Order { get; }
    Task ApplyAsync(IDbConnectionProvider db, CancellationToken ct);
}

public interface IDbConnectionProvider
{
    /// <summary>
    /// The shared SQLiteAsyncConnection. Typed as object to keep Player.Core free of SQLite.
    /// </summary>
    Task<object> GetConnectionAsync(CancellationToken ct);
}
