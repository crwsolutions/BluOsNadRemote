namespace BluOsNadRemote.App.Repositories;
internal static class RepositoriesExtensions
{
    internal static MauiAppBuilder ConfigurePreferences(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton(Preferences.Default);
        return builder;
    }

    internal static MauiAppBuilder ConfigureRepositories(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<EndpointRepository>();
        builder.Services.AddSingleton<CultureOverrideRepository>();
        builder.Services.AddSingleton<ThemeOverrideRepository>();

        return builder;
    }
}
