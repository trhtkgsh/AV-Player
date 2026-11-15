using Player.App.Composition;
using Player.App.Engine;
using Player.App.Features.DefaultsSettings;
using Player.App.Features.Explorer;
using Player.App.Features.InterfaceSettings;
using Player.App.Features.PlaybackSettings;
using Player.App.Features.PlayerHud;
using Player.App.Gestures;
using Player.App.Localization;
using Player.App.Platforms.Android.Library;
using Player.App.Platforms.Android.Thumbnails;
using Player.App.Subtitles;
using Player.App.Surface;
using Player.App.Theming;
using Player.Core.Contracts;
using Player.Core.Logic.Playback;
using Player.Core.Logic.StateRecording;
using Player.Data.Database;
using Player.Data.GlobalDefaultsStorage;
using Player.Data.PlaybackStateStorage;
using Player.Data.UiSettingsStorage;
using Microsoft.Maui.Storage;

namespace Player.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		builder.UseVideoSurface();
		builder.UseGestureOverlay();

		builder.Services.AddPlayerDatabase(Path.Combine(FileSystem.AppDataDirectory, "player.db3"));
		builder.Services.AddAppInfrastructure();
		builder.Services.AddPlaybackStateStore();
		builder.Services.AddGlobalDefaults();
		builder.Services.AddUiSettings();
		builder.Services.AddSingleton<IPlaybackStateRecorder, PlaybackStateRecorder>();
		builder.Services.AddPlayerEngine();
		builder.Services.AddMediaLibrary();
		builder.Services.AddThumbnailCache();
		builder.Services.AddTheming();
		builder.Services.AddSingleton<IPlaybackCoordinator, PlaybackCoordinator>();
		builder.Services.AddSubtitlesAudio();
		builder.Services.AddGestures();
		builder.Services.AddLocalization();
		builder.Services.AddExplorer();
		builder.Services.AddPlayerHud();
		builder.Services.AddPlaybackSettings();
		builder.Services.AddGlobalDefaultsUi();
		builder.Services.AddUiSettingsUi();

		return builder.Build();
	}
}
