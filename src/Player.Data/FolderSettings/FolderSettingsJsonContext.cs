using System.Text.Json;
using System.Text.Json.Serialization;

namespace Player.Data.FolderSettingsStorage;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Dictionary<string, FolderSettingsDto>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
internal sealed partial class FolderSettingsJsonContext : JsonSerializerContext;
