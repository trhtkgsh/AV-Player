using System.Text.Json.Serialization;

namespace Player.Data.GlobalDefaultsStorage;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GlobalDefaultsDto))]
internal sealed partial class GlobalDefaultsJsonContext : JsonSerializerContext;
