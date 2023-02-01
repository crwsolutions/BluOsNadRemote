﻿namespace BluOsNadRemote.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<PlayerViewModel>();

		builder.Services.AddSingleton<PlayerPage>();

		builder.Services.AddSingleton<PresetsViewModel>();

		builder.Services.AddSingleton<PresetsPage>();

		builder.Services.AddSingleton<BrowseViewModel>();

		builder.Services.AddSingleton<BrowsePage>();

		builder.Services.AddSingleton<AdvancedViewModel>();

		builder.Services.AddSingleton<AdvancedPage>();

		builder.Services.AddSingleton<SettingsViewModel>();

		builder.Services.AddSingleton<SettingsPage>();

		return builder.Build();
	}
}
