using System.Text.Json;

namespace Player.Data.UiSettingsStorage;

internal sealed class UiSettingsDto
{
	public int? HudAutoHideSeconds { get; set; }
	public int? SkipSeconds { get; set; }
	public bool? ShowClock { get; set; }
	public bool? ShowBattery { get; set; }

	// Enum fields are read as raw JSON, so each one can fall back on its own (see UiSettingsStore.ParseEnum).
	// They are written as names, for example "Duration, Size" or "Hebrew".
	public JsonElement? ExplorerFields { get; set; }
	public JsonElement? ViewMode { get; set; }
	public JsonElement? SortField { get; set; }
	public JsonElement? SortDirection { get; set; }
	public int? NewTagDays { get; set; }
	public JsonElement? Language { get; set; }
	public JsonElement? Layout { get; set; }
	public JsonElement? ExplorerFontScale { get; set; }
	public JsonElement? IncludeAudioOnlyFolders { get; set; }
	public JsonElement? LongPressSpeed { get; set; }
}
