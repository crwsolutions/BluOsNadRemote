namespace BluOsNadRemote.App.ViewModels;
internal static class ViewModelsExtensions
{
    internal static MauiAppBuilder ConfigureViewModels(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<PlayerViewModel>();
        builder.Services.AddSingleton<PresetsViewModel>();
        builder.Services.AddSingleton<BrowseViewModel>();
        builder.Services.AddSingleton<AdvancedViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPlayerViewModel>();
        builder.Services.AddSingleton<SettingsMoreViewModel>();
        builder.Services.AddTransient<QueueViewModel>();

        return builder;
    }
}
