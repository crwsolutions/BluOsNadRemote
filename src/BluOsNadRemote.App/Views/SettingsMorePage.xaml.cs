namespace BluOsNadRemote.App.Views;

public partial class SettingsMorePage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private SettingsMoreViewModel ViewModel => BindingContext as SettingsMoreViewModel;

    partial void PreConstruct() => InitializeComponent();

    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        var radioButton = (RadioButton)sender;
        ViewModel.SetCulture((string)radioButton.Value);
    }
}