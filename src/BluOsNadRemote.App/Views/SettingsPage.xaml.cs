namespace BluOsNadRemote.App.Views;

public partial class SettingsPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private SettingsViewModel ViewModel => (SettingsViewModel)BindingContext;

    partial void PreConstruct() => InitializeComponent();
}
