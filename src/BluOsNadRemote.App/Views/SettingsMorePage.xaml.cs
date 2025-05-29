namespace BluOsNadRemote.App.Views;

public partial class SettingsMorePage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private SettingsMoreViewModel ViewModel => (SettingsMoreViewModel)BindingContext;

    partial void PreConstruct() => InitializeComponent();

    private void Language_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        var radioButton = (RadioButton)sender;
        ViewModel.SetCulture((string)radioButton.Value);
    }

    private void Theme_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        var radioButton = (RadioButton)sender;
        ViewModel.SetTheme((string)radioButton.Value);
    }
}