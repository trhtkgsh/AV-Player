using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using LibVLCSharp.Shared;

namespace EngineSpike;

/// <summary>
/// Foreground service (mediaPlayback) that owns the MediaSession, audio focus
/// and the becoming-noisy receiver. It only wraps the engine singleton.
/// </summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class SpikePlaybackService : Service, AudioManager.IOnAudioFocusChangeListener
{
	const string ChannelId = "spike_playback";
	const int NotificationId = 1;

	readonly SpikeEngine _engine = SpikeEngine.Instance;
	MediaSession? _session;
	NoisyReceiver? _noisyReceiver;
	AudioFocusRequestClass? _focusRequest;

	public static void Start()
	{
		var context = Android.App.Application.Context;
		context.StartForegroundService(new Intent(context, typeof(SpikePlaybackService)));
	}

	public static void Stop()
	{
		var context = Android.App.Application.Context;
		context.StopService(new Intent(context, typeof(SpikePlaybackService)));
	}

	public override IBinder? OnBind(Intent? intent) => null;

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		if (_session is not null)
			return StartCommandResult.Sticky;

		var notifications = (NotificationManager)GetSystemService(NotificationService)!;
		notifications.CreateNotificationChannel(new NotificationChannel(ChannelId, "Playback", NotificationImportance.Low));

		_session = new MediaSession(this, "EngineSpike");
		_session.SetCallback(new SessionCallback(_engine));
		_session.SetMetadata(new MediaMetadata.Builder()
			.PutString(MediaMetadata.MetadataKeyTitle, "sample.mp4")!
			.PutLong(MediaMetadata.MetadataKeyDuration, _engine.Player.Length)!
			.Build());
		PublishState();
		_session.Active = true;

		var notification = new Notification.Builder(this, ChannelId)
			.SetContentTitle("EngineSpike")
			.SetContentText("sample.mp4")
			.SetSmallIcon(Android.Resource.Drawable.IcMediaPlay)
			.SetOngoing(true)
			.SetStyle(new Notification.MediaStyle().SetMediaSession(_session.SessionToken))
			.Build();

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
			StartForeground(NotificationId, notification, ForegroundService.TypeMediaPlayback);
		else
			StartForeground(NotificationId, notification);

		_engine.Player.Playing += OnPlayerStateChanged;
		_engine.Player.Paused += OnPlayerStateChanged;
		_engine.Player.Stopped += OnPlayerStateChanged;

		var audio = (AudioManager)GetSystemService(AudioService)!;
		_focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
			.SetAudioAttributes(new AudioAttributes.Builder()
				.SetUsage(AudioUsageKind.Media)!
				.SetContentType(AudioContentType.Movie)!
				.Build()!)
			.SetOnAudioFocusChangeListener(this)
			.Build()!;
		SpikeEngine.Log($"service: audio focus request = {audio.RequestAudioFocus(_focusRequest)}");

		_noisyReceiver = new NoisyReceiver(_engine);
		var filter = new IntentFilter(AudioManager.ActionAudioBecomingNoisy);
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
			RegisterReceiver(_noisyReceiver, filter, ReceiverFlags.NotExported);
		else
			RegisterReceiver(_noisyReceiver, filter);

		SpikeEngine.Log("service: started in foreground, media session active");
		return StartCommandResult.Sticky;
	}

	public override void OnDestroy()
	{
		_engine.Player.Playing -= OnPlayerStateChanged;
		_engine.Player.Paused -= OnPlayerStateChanged;
		_engine.Player.Stopped -= OnPlayerStateChanged;

		if (_noisyReceiver is not null)
			UnregisterReceiver(_noisyReceiver);
		if (_focusRequest is not null)
			((AudioManager)GetSystemService(AudioService)!).AbandonAudioFocusRequest(_focusRequest);

		_session?.Release();
		_session = null;
		SpikeEngine.Log("service: destroyed");
		base.OnDestroy();
	}

	public void OnAudioFocusChange(AudioFocus focusChange)
	{
		SpikeEngine.Log($"service: audio focus change = {focusChange}");
		if (focusChange is AudioFocus.Loss or AudioFocus.LossTransient)
			ThreadPool.QueueUserWorkItem(_ => _engine.Player.SetPause(true));
	}

	void OnPlayerStateChanged(object? sender, EventArgs e) => SpikeEngine.Post(PublishState);

	void PublishState()
	{
		var player = _engine.Player;
		var code = player.State switch
		{
			VLCState.Playing => PlaybackStateCode.Playing,
			VLCState.Paused => PlaybackStateCode.Paused,
			VLCState.Buffering or VLCState.Opening => PlaybackStateCode.Buffering,
			_ => PlaybackStateCode.Stopped,
		};

		_session?.SetPlaybackState(new PlaybackState.Builder()
			.SetActions(PlaybackState.ActionPlay | PlaybackState.ActionPause | PlaybackState.ActionPlayPause | PlaybackState.ActionSeekTo)!
			.SetState(code, Math.Max(0, player.Time), code == PlaybackStateCode.Playing ? 1f : 0f)!
			.Build());
	}

	sealed class SessionCallback(SpikeEngine engine) : MediaSession.Callback
	{
		public override void OnPlay()
		{
			SpikeEngine.Log("session: OnPlay");
			ThreadPool.QueueUserWorkItem(_ => engine.Player.SetPause(false));
		}

		public override void OnPause()
		{
			SpikeEngine.Log("session: OnPause");
			ThreadPool.QueueUserWorkItem(_ => engine.Player.SetPause(true));
		}

		public override void OnSeekTo(long pos)
		{
			SpikeEngine.Log($"session: OnSeekTo {pos}");
			ThreadPool.QueueUserWorkItem(_ => engine.Player.Time = pos);
		}
	}

	sealed class NoisyReceiver(SpikeEngine engine) : BroadcastReceiver
	{
		public override void OnReceive(Context? context, Intent? intent)
		{
			SpikeEngine.Log("service: audio becoming noisy -> pause");
			ThreadPool.QueueUserWorkItem(_ => engine.Player.SetPause(true));
		}
	}
}
