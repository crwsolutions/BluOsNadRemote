namespace BluOsNadRemote.App.Views;
internal static class ViewsExtensions
{
    internal static MauiAppBuilder ConfigurePages(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<PlayerPage>();
        builder.Services.AddSingleton<PresetsPage>();
        builder.Services.AddSingleton<BrowsePage>();
        builder.Services.AddSingleton<AdvancedPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsPlayerPage>();
        builder.Services.AddSingleton<SettingsMorePage>();
        builder.Services.AddTransient<QueuePage>();

        return builder;
    }
}
