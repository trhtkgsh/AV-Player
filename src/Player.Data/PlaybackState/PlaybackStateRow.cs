using SQLite;

namespace Player.Data.PlaybackStateStorage;

/// <summary>One row of table playback_state. The table is created by <see cref="PlaybackStateMigration"/>.</summary>
[Table("playback_state")]
public sealed class PlaybackStateRow
{
    [PrimaryKey, Column("file_key")]
    public string FileKey { get; set; } = "";

    [Column("media_store_id")]
    public long MediaStoreId { get; set; }

    [Column("position_ms")]
    public long PositionMs { get; set; }

    [Column("duration_ms")]
    public long DurationMs { get; set; }

    [Column("finished")]
    public bool Finished { get; set; }

    [Column("last_played_utc")]
    public long LastPlayedUtc { get; set; }

    [Column("overrides_json")]
    public string? OverridesJson { get; set; }
}
