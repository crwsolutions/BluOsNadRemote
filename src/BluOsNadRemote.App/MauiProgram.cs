using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Services;
using CommunityToolkit.Maui;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace BluOsNadRemote.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp() =>
        MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigurePreferences()
            .ConfigureRepositories()
            .ConfigureServices()
            .ConfigurePages()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("CrwMedia.ttf", "CrwMedia");
            })
            .Build();
}
