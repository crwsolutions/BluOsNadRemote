namespace BluOsNadRemote.App.Views;

public partial class AdvancedPage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private AdvancedViewModel ViewModel => BindingContext as AdvancedViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected override void OnAppearing()
    {
        if (ViewModel?.IsBusy == false)
        {
            ViewModel.IsBusy = true;
        }
    }

    protected override void OnDisappearing()
    {
        ViewModel?.Dispose();
    }
}
