using Android.Content;
using Android.Provider;

namespace EngineSpike;

public partial class MainPage : ContentPage
{
	readonly SpikeEngine _engine = SpikeEngine.Instance;
	bool _started;

	public MainPage()
	{
		InitializeComponent();
		// Attach before the first layout pass: LibVLCSharp reports the window size to LibVLC
		// only from a layout-change callback, so a late attach renders unscaled at the bottom-left.
		Video.MediaPlayer = _engine.Player;
		SpikeCommands.Received += command => MainThread.BeginInvokeOnMainThread(() => Run(command));
		_engine.StateChanged += () => MainThread.BeginInvokeOnMainThread(UpdateStatus);
		Video.Loaded += OnVideoLoaded;
	}

	async void OnVideoLoaded(object? sender, EventArgs e)
	{
		SpikeEngine.Log($"video view attached, platform view = {DescribePlatformView()}");

		if (_started)
			return;
		_started = true;

		var path = Path.Combine(FileSystem.CacheDirectory, "sample.mp4");
		if (!File.Exists(path))
		{
			await using var source = await FileSystem.OpenAppPackageFileAsync("sample.mp4");
			await using var target = File.Create(path);
			await source.CopyToAsync(target);
		}

		await Task.Run(() => _engine.PlayPath(path, hardware: true));
	}

	void OnCommandClicked(object? sender, EventArgs e)
	{
		if (sender is Button { CommandParameter: string command })
			Run(command);
	}

	void Run(string command)
	{
		SpikeEngine.Log($"command {command}");
		switch (command)
		{
			// Engine calls stay off the UI thread: LibVLC Stop() blocks and can deadlock with the vout.
			case "hw": Task.Run(() => _engine.SwitchDecoder(true)); break;
			case "sw": Task.Run(() => _engine.SwitchDecoder(false)); break;
			case "toggle": Task.Run(() => _engine.Player.Pause()); break;
			case "zoom": ApplyZoom(2f, 0.25f, -0.15f); break;
			case "zoomreset": ApplyZoom(1f, 0f, 0f); break;
			case "service": SpikePlaybackService.Start(); break;
			case "stopservice": SpikePlaybackService.Stop(); break;
			case "playfd": Task.Run(PlayFirstMediaStoreVideo); break;
			case "revideo": Task.Run(_engine.ReattachVideo); break;
			case "reattach": ReattachSurface(); break;
			default: SpikeEngine.Log($"unknown command {command}"); break;
		}
	}

	/// <summary>
	/// Recovery after the activity was backgrounded: LibVLC's AWindow detaches itself when the surface is
	/// destroyed, so re-attach the view, force a layout change (the only path that reports the window
	/// size) and restart the video ES.
	/// </summary>
	void ReattachSurface()
	{
		Video.MediaPlayer = null;
		Video.MediaPlayer = _engine.Player;
		if (Video.Handler?.PlatformView is Android.Views.View view)
		{
			int left = view.Left, top = view.Top, right = view.Right, bottom = view.Bottom;
			view.Layout(left, top, right - 1, bottom);
			view.Layout(left, top, right, bottom);
		}
		Task.Run(_engine.ReattachVideo);
	}

	/// <summary>Zoom/pan by transforming the native surface view; pan is a fraction of the view size.</summary>
	void ApplyZoom(float scale, float panX, float panY)
	{
		if (Video.Handler?.PlatformView is not Android.Views.View view)
			return;

		view.ScaleX = scale;
		view.ScaleY = scale;
		view.TranslationX = panX * view.Width;
		view.TranslationY = panY * view.Height;
		SpikeEngine.Log($"zoom scale={scale} tx={view.TranslationX} ty={view.TranslationY} view={view.Width}x{view.Height}");
	}

	/// <summary>Plays by MediaStore id through a file descriptor, never by path.</summary>
	void PlayFirstMediaStoreVideo()
	{
		var resolver = Android.App.Application.Context.ContentResolver!;
		var collection = MediaStore.Video.Media.ExternalContentUri!;
		using var cursor = resolver.Query(collection, [IBaseColumns.Id, MediaStore.IMediaColumns.DisplayName], null, null, null);
		if (cursor is null || !cursor.MoveToFirst())
		{
			SpikeEngine.Log("playfd: MediaStore returned no videos (missing permission or empty library)");
			return;
		}

		var id = cursor.GetLong(0);
		var uri = ContentUris.WithAppendedId(collection, id);
		SpikeEngine.Log($"playfd: id={id} name={cursor.GetString(1)} uri={uri}");
		_engine.PlayFileDescriptor(() => resolver.OpenFileDescriptor(uri, "r")!.DetachFd(), hardware: true);
	}

	string DescribePlatformView()
	{
		var type = Video.Handler?.PlatformView?.GetType();
		return type is null ? "null" : $"{type.FullName} : {type.BaseType?.FullName}";
	}

	void UpdateStatus()
	{
		var player = _engine.Player;
		Status.Text = $"{player.State}  {player.Time / 1000.0:F1}s / {player.Length / 1000.0:F1}s  " +
			$"decoder={(_engine.HardwareDecoding ? "HW" : "SW")} ({_engine.VideoDecoderModule})  vout={player.VoutCount}";
	}
}
