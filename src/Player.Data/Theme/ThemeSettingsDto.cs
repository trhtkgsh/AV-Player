using System.Text.Json;

namespace Player.Data.ThemeStorage;

/// <summary>What is read from the <c>theme</c> row. Raw elements, so one wrongly typed field never discards the others.</summary>
internal sealed class ThemeSettingsReadDto
{
	public JsonElement? Theme { get; set; }
	public JsonElement? AccentColorArgb { get; set; }
	public JsonElement? HudOpacity { get; set; }
}

/// <summary>What is written to the <c>theme</c> row. The theme goes by name.</summary>
internal sealed class ThemeSettingsWriteDto
{
	public string? Theme { get; set; }
	public uint? AccentColorArgb { get; set; }
	public double? HudOpacity { get; set; }
}
