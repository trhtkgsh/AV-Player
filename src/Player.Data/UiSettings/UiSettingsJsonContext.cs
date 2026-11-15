using System.Text.Json.Serialization;

namespace Player.Data.UiSettingsStorage;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(UiSettingsDto))]
internal sealed partial class UiSettingsJsonContext : JsonSerializerContext;
