using CommunityToolkit.Maui;

namespace BluOsNadRemote.App.Views;
internal static class ViewsExtensions
{
    internal static MauiAppBuilder ConfigurePages(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<PlayerPage, PlayerViewModel>();
        builder.Services.AddSingleton<PresetsPage, PresetsViewModel>();
        builder.Services.AddSingletonWithShellRoute<BrowsePage, BrowseViewModel>(nameof(BrowsePage));
        builder.Services.AddSingleton<AdvancedPage, AdvancedViewModel>();
        builder.Services.AddSingletonWithShellRoute<SettingsPage, SettingsViewModel>(nameof(SettingsPage));
        builder.Services.AddSingletonWithShellRoute<SettingsPlayerPage, SettingsPlayerViewModel>(nameof(SettingsPlayerPage));
        builder.Services.AddSingletonWithShellRoute<SettingsMorePage, SettingsMoreViewModel>(nameof(SettingsMorePage));
        builder.Services.AddSingletonWithShellRoute<QueuePage, QueueViewModel>(nameof(QueuePage));

        return builder;
    }
}
