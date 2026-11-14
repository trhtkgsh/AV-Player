using SQLite;

namespace Player.Data.Database;

[Table("settings")]
public sealed class SettingsRow
{
	[PrimaryKey]
	[Column("key")]
	public string Key { get; set; } = string.Empty;

	[Column("json")]
	public string Json { get; set; } = string.Empty;

	[Column("updated_utc")]
	public long UpdatedUtc { get; set; }
}
