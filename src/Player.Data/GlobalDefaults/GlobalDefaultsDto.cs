namespace Player.Data.GlobalDefaultsStorage;

internal sealed class GlobalDefaultsDto
{
	public float? Speed { get; set; }
	public string? Decoder { get; set; }          // enum name
	public string? Aspect { get; set; }           // enum name
	public string? Repeat { get; set; }           // enum name
	public bool? ResumeFromLastPosition { get; set; }
	public bool? SubtitlesEnabled { get; set; }
	public string? PreferredAudioLanguage { get; set; }
	public string? PreferredSubtitleLanguage { get; set; }
	public long? AudioDelayMs { get; set; }
	public long? SubtitleDelayMs { get; set; }
	public SubtitleStyleDto? SubtitleStyle { get; set; }
	public bool? BackgroundAudio { get; set; }
	public bool? PictureInPictureOnLeave { get; set; }
}

internal sealed class SubtitleStyleDto
{
	public double? FontScale { get; set; }
	public uint? TextColorArgb { get; set; }
	public uint? OutlineColorArgb { get; set; }
	public int? OutlineThickness { get; set; }
	public double? BackgroundOpacity { get; set; }
	public bool? Bold { get; set; }
	public double? BottomMarginFraction { get; set; }
}
