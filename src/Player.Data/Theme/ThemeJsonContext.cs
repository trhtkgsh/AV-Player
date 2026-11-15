using System.Text.Json.Serialization;

namespace Player.Data.ThemeStorage;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ThemeSettingsReadDto))]
[JsonSerializable(typeof(ThemeSettingsWriteDto))]
internal sealed partial class ThemeJsonContext : JsonSerializerContext;
