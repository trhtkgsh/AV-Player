using System.Globalization;
using System.Text.RegularExpressions;
using LibVLCSharp.Shared;

namespace EngineSpike;

/// <summary>
/// Process-wide engine singleton. Never owned by a page, so the foreground
/// service and the UI share the same LibVLC/MediaPlayer instance.
/// </summary>
public sealed partial class SpikeEngine
{
	public const string Tag = "SPIKE";

	public static SpikeEngine Instance { get; } = new();

	readonly LibVLC _libVlc;
	readonly Timer _ticker;
	Func<Media>? _mediaFactory;

	public MediaPlayer Player { get; }
	public bool HardwareDecoding { get; private set; } = true;
	public string VideoDecoderModule { get; private set; } = "?";

	public event Action? StateChanged;

	SpikeEngine()
	{
		Core.Initialize();
		_libVlc = new LibVLC(enableDebugLogs: true);
		_libVlc.Log += OnLibVlcLog;
		Player = new MediaPlayer(_libVlc);

		// LibVLC raises events while holding its own locks (Stopped fires inside Stop()).
		// Calling back into the player from a handler aborts the process, so always hop threads first.
		Player.Playing += (_, _) => Post(() => Report("event Playing"));
		Player.Paused += (_, _) => Post(() => Report("event Paused"));
		Player.Stopped += (_, _) => Post(() => Report("event Stopped"));
		Player.EncounteredError += (_, _) => Post(() => Report("event EncounteredError"));
		Player.Vout += (_, e) => Log($"event Vout count={e.Count}");
		Player.EndReached += (_, _) => Post(() => Restart(0));

		Log($"LibVLC {_libVlc.Version} initialized");
		_ticker = new Timer(_ => Report("tick"), null, 2000, 2000);
	}

	public static void Log(string message) => Android.Util.Log.Info(Tag, message);

	public static void Post(Action action) => ThreadPool.QueueUserWorkItem(_ => action());

	public void PlayPath(string path, bool hardware)
	{
		_mediaFactory = () => new Media(_libVlc, path, FromType.FromPath);
		HardwareDecoding = hardware;
		Log($"play path={path} hw={hardware}");
		Restart(0);
	}

	public void PlayFileDescriptor(Func<int> openFd, bool hardware)
	{
		_mediaFactory = () => new Media(_libVlc, openFd());
		HardwareDecoding = hardware;
		Log($"play fd hw={hardware}");
		Restart(0);
	}

	/// <summary>LibVLC 3 cannot swap decoders in place: stop, rebuild the media, resume at the same position.</summary>
	public void SwitchDecoder(bool hardware)
	{
		var position = Player.Time;
		Log($"switch decoder -> {(hardware ? "HW" : "SW")} at {position}ms");
		HardwareDecoding = hardware;
		Player.Stop();
		Restart(position);
	}

	/// <summary>Light recovery after the surface was destroyed and recreated: restart only the video ES.</summary>
	public void ReattachVideo()
	{
		var track = Player.VideoTrack;
		Log($"reattach video: track={track}");
		Player.SetVideoTrack(-1);
		Player.SetVideoTrack(track);
	}

	void Restart(long startMs)
	{
		if (_mediaFactory is null)
			return;

		VideoDecoderModule = "?";
		using var media = _mediaFactory();
		media.AddOption(HardwareDecoding ? ":codec=mediacodec_ndk,all" : ":codec=all");
		if (startMs > 0)
			media.AddOption(string.Create(CultureInfo.InvariantCulture, $":start-time={startMs / 1000.0:F3}"));
		Player.Play(media);
	}

	void OnLibVlcLog(object? sender, LogEventArgs e)
	{
		var match = DecoderRegex().Match(e.Message);
		if (match.Success && match.Groups[1].Value == "video")
			VideoDecoderModule = match.Groups[2].Value;

		if (match.Success || e.Level >= LogLevel.Warning || Interlocked.Increment(ref _verboseBudget) < 500)
			Android.Util.Log.Info("SPIKEVLC", $"[{e.Level}] {Path.GetFileName(e.SourceFile)}:{e.SourceLine} {e.Message}");
	}

	int _verboseBudget;

	void Report(string reason)
	{
		Log($"{reason} | state={Player.State} time={Player.Time}ms len={Player.Length}ms vout={Player.VoutCount} hw={HardwareDecoding} vdec={VideoDecoderModule}");
		StateChanged?.Invoke();
	}

	[GeneratedRegex("using (video|audio) decoder module \"([^\"]+)\"")]
	private static partial Regex DecoderRegex();
}

public static class SpikeCommands
{
	public static event Action<string>? Received;

	public static void Dispatch(string? command)
	{
		if (!string.IsNullOrEmpty(command))
			Received?.Invoke(command);
	}
}
