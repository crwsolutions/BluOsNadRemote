namespace BluOsNadRemote.App.Views;

public partial class PresetsPage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private PresetsViewModel ViewModel => BindingContext as PresetsViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected override void OnAppearing()
    {
        if (ViewModel?.IsBusy == false)
        {
            ViewModel.IsBusy = true;
        }
    }
}
