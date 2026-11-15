namespace Player.Data.UiSettingsStorage;

internal sealed class UiSettingsDto
{
	public int? HudAutoHideSeconds { get; set; }
	public int? SkipSeconds { get; set; }
	public bool? ShowClock { get; set; }
	public bool? ShowBattery { get; set; }
	public string? ExplorerFields { get; set; }   // enum names, for example "Duration, Size"
	public string? ViewMode { get; set; }
	public string? SortField { get; set; }
	public string? SortDirection { get; set; }
	public int? NewTagDays { get; set; }
}
