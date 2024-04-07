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
            .ConfigurePreferences()
            .ConfigureRepositories()
            .ConfigureServices()
            .ConfigureViewModels()
            .ConfigurePages()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("CrwMedia.ttf", "CrwMedia");
            });

        return builder.Build();
    }
}
