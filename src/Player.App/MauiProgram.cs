using Player.App.Composition;
using Player.Data.Database;
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

		builder.Services.AddPlayerDatabase(Path.Combine(FileSystem.AppDataDirectory, "player.db3"));
		builder.Services.AddAppInfrastructure();

		return builder.Build();
	}
}
