using System.Text.Json;
using System.Text.Json.Serialization;
using Player.Core.Models;

namespace Player.Data.PlaybackState;

/// <summary>
/// The nullable per-file fields of <see cref="Player.Core.Models.PlaybackState"/>, stored together as
/// one JSON text. A null field is not written. New fields need no migration.
/// </summary>
public sealed class PlaybackStateOverrides
{
    public float? Speed { get; set; }
    public DecoderMode? Decoder { get; set; }
    public AspectMode? Aspect { get; set; }
    public int? AudioTrackId { get; set; }
    public long? AudioDelayMs { get; set; }
    public int? SubtitleTrackId { get; set; }
    public long? SubtitleDelayMs { get; set; }
    public string? ExternalSubtitleUri { get; set; }

    [JsonIgnore]
    public bool IsEmpty =>
        Speed is null && Decoder is null && Aspect is null && AudioTrackId is null &&
        AudioDelayMs is null && SubtitleTrackId is null && SubtitleDelayMs is null &&
        ExternalSubtitleUri is null;

    public static PlaybackStateOverrides From(Player.Core.Models.PlaybackState state) => new()
    {
        Speed = state.Speed,
        Decoder = state.Decoder,
        Aspect = state.Aspect,
        AudioTrackId = state.AudioTrackId,
        AudioDelayMs = state.AudioDelayMs,
        SubtitleTrackId = state.SubtitleTrackId,
        SubtitleDelayMs = state.SubtitleDelayMs,
        ExternalSubtitleUri = state.ExternalSubtitleUri,
    };

    /// <summary>Returns null (SQL NULL) when every override is null.</summary>
    public string? ToJson() =>
        IsEmpty ? null : JsonSerializer.Serialize(this, PlaybackStateJsonContext.Default.PlaybackStateOverrides);

    /// <summary>Never throws. Missing, invalid or unreadable JSON gives "no overrides".</summary>
    public static PlaybackStateOverrides Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new PlaybackStateOverrides();

        try
        {
            return JsonSerializer.Deserialize(json, PlaybackStateJsonContext.Default.PlaybackStateOverrides)
                ?? new PlaybackStateOverrides();
        }
        catch (JsonException)
        {
            return new PlaybackStateOverrides();
        }
        catch (InvalidOperationException)
        {
            return new PlaybackStateOverrides();
        }
        catch (NotSupportedException)
        {
            return new PlaybackStateOverrides();
        }
    }
}

[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PlaybackStateOverrides))]
public sealed partial class PlaybackStateJsonContext : JsonSerializerContext
{
}
