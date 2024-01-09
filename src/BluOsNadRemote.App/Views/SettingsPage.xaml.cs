namespace BluOsNadRemote.App.Views;

public partial class SettingsPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private SettingsViewModel ViewModel => BindingContext as SettingsViewModel;

    partial void PreConstruct() => InitializeComponent();
}
