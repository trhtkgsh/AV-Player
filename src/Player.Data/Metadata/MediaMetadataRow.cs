using SQLite;

namespace Player.Data.Metadata;

/// <summary>One row of table media_metadata. The table is created by <see cref="MediaMetadataMigration"/>.</summary>
[Table("media_metadata")]
public sealed class MediaMetadataRow
{
    [PrimaryKey, Column("file_key")]
    public string FileKey { get; set; } = "";

    [Column("media_store_id")]
    public long MediaStoreId { get; set; }

    [Column("duration_ms")]
    public long DurationMs { get; set; }

    [Column("width")]
    public int Width { get; set; }

    [Column("height")]
    public int Height { get; set; }

    [Column("thumbnail_path")]
    public string? ThumbnailPath { get; set; }

    [Column("failed")]
    public bool Failed { get; set; }

    [Column("created_utc")]
    public long CreatedUtc { get; set; }
}
