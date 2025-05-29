namespace BluOsNadRemote.App.Services;
internal static class ServicesExtensions
{
    internal static MauiAppBuilder ConfigureServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<LanguageService>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<BluPlayerService>();
        builder.Services.AddSingleton<NadTelnetService>();
        builder.Services.AddSingleton<NoConnectionDialogService>();

        return builder;
    }
}
