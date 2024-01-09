namespace BluOsNadRemote.App.Views;

public partial class SettingsPlayerPage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private SettingsPlayerViewModel ViewModel => BindingContext as SettingsPlayerViewModel;

    partial void PreConstruct() => InitializeComponent();
}