using System.Text.Json.Serialization;

namespace Player.Data.UiSettingsStorage;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(UiSettingsDto))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
internal sealed partial class UiSettingsJsonContext : JsonSerializerContext;
