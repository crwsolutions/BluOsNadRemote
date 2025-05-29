namespace BluOsNadRemote.App.Views;

public partial class SettingsPlayerPage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private SettingsPlayerViewModel ViewModel => (SettingsPlayerViewModel)BindingContext;

    partial void PreConstruct() => InitializeComponent();
}