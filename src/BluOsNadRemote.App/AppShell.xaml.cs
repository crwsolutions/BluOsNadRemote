﻿namespace BluOsNadRemote.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(QueuePage), typeof(QueuePage));
        Routing.RegisterRoute(nameof(BrowsePage), typeof(BrowsePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(SettingsPlayerPage), typeof(SettingsPlayerPage));
        Routing.RegisterRoute(nameof(SettingsMorePage), typeof(SettingsMorePage));
    }
}
