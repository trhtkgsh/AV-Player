using System.Text.Json.Serialization;
using Player.Core.Models;

namespace Player.Data.UiSettingsStore;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(UiSettings))]
internal sealed partial class UiSettingsJsonContext : JsonSerializerContext;
