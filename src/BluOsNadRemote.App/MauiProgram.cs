using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Services;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace BluOsNadRemote.App;

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
                fonts.AddFont("CrwMedia.ttf", "CrwMedia");
            });

        builder.Services.AddSingleton<PlayerViewModel>();
        builder.Services.AddSingleton<PlayerPage>();
        builder.Services.AddSingleton<PresetsViewModel>();
        builder.Services.AddSingleton<PresetsPage>();
        builder.Services.AddSingleton<BrowseViewModel>();
        builder.Services.AddSingleton<BrowsePage>();
        builder.Services.AddSingleton<AdvancedViewModel>();
        builder.Services.AddSingleton<AdvancedPage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsPlayerPage>();
        builder.Services.AddTransient<SettingsPlayerViewModel>();
        builder.Services.AddSingleton<SettingsMorePage>();
        builder.Services.AddSingleton<SettingsMoreViewModel>();
        builder.Services.AddTransient<QueueViewModel>();
        builder.Services.AddTransient<QueuePage>();

        builder.Services.AddSingleton(Preferences.Default);

        builder.Services.AddSingleton<EndpointRepository>();
        builder.Services.AddSingleton<CultureOverrideRepository>();

        builder.Services.AddSingleton<LanguageService>();
        builder.Services.AddSingleton<BluPlayerService>();
        builder.Services.AddSingleton<NadTelnetService>();

        return builder.Build();
    }
}
