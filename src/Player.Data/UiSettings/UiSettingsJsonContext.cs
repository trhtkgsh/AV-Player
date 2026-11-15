using System.Text.Json.Serialization;

namespace Player.Data.UiSettingsStorage;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(UiSettingsDto))]
[JsonSerializable(typeof(string))]
internal sealed partial class UiSettingsJsonContext : JsonSerializerContext;
