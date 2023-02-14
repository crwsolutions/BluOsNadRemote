namespace BluOsNadRemote.App.Views;

public partial class SettingsPage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private SettingsViewModel ViewModel => BindingContext as SettingsViewModel;

    partial void PreConstruct() => InitializeComponent();
}
