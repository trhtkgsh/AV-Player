using System.Text.Json;

namespace Player.Data.UiSettingsStorage;

internal sealed class UiSettingsDto
{
	// Every field is read as raw JSON, so each one can fall back on its own (see UiSettingsStore.FromDto).
	// Numbers and booleans are written as JSON numbers and booleans, enums as names,
	// for example "Duration, Size" or "Hebrew".
	// SchemaVersion is not a model field. A row without it, or below 2, is an older row (see UiSettingsStore).
	public JsonElement? SchemaVersion { get; set; }
	public JsonElement? HudAutoHideSeconds { get; set; }
	public JsonElement? SkipSeconds { get; set; }
	public JsonElement? ShowClock { get; set; }
	public JsonElement? ShowBattery { get; set; }
	public JsonElement? ExplorerFields { get; set; }
	public JsonElement? ViewMode { get; set; }
	public JsonElement? SortField { get; set; }
	public JsonElement? SortDirection { get; set; }
	public JsonElement? NewTagDays { get; set; }
	public JsonElement? Language { get; set; }
	public JsonElement? Layout { get; set; }
	public JsonElement? ItemSize { get; set; }
	public JsonElement? ExplorerFontScale { get; set; }
	public JsonElement? IncludeAudioOnlyFolders { get; set; }
	public JsonElement? LongPressSpeed { get; set; }
	public JsonElement? OverlayProgressLine { get; set; }
	public JsonElement? OverlayProgressLineEdge { get; set; }
	public JsonElement? OverlayProgressLineThickness { get; set; }
	public JsonElement? OverlayClock { get; set; }
	public JsonElement? OverlayBattery { get; set; }
	public JsonElement? OverlayTime { get; set; }
	public JsonElement? OverlayCorner { get; set; }
	public JsonElement? OverlayClockCorner { get; set; }
	public JsonElement? OverlayBatteryCorner { get; set; }
	public JsonElement? OverlayMarginDp { get; set; }
	public JsonElement? OverlayFontSize { get; set; }
	public JsonElement? OverlayColorArgb { get; set; }
}
