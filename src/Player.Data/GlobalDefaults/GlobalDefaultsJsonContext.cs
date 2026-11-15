using System.Text.Json.Serialization;
using Player.Core.Models;

namespace Player.Data.GlobalDefaultsStore;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(GlobalDefaults))]
internal sealed partial class GlobalDefaultsJsonContext : JsonSerializerContext;
