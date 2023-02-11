namespace BluOsNadRemote.App.Views;

public partial class PlayerPage : ContentPage
{
    [Dependency(nameof(BindingContext))]
    private PlayerViewModel ViewModel => BindingContext as PlayerViewModel;

    partial void PreConstruct() => InitializeComponent();

    volatile bool run;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!ViewModel.IsBusy)
        {
            ViewModel.IsBusy = true;
        }

        run = true;
        Device.StartTimer(new TimeSpan(0, 0, 1), () =>
        {
            if (run)
            {
                ViewModel.UpdateProgress();
                return true;
            }
            else
            {
                Debug.WriteLine("Returning false in Device timer");
                return false;
            }
        });

        Debug.WriteLine("OnAppearing --> PlayerViewModel found");
    }

    protected override void OnDisappearing()
    {
        run = false;
        ViewModel?.Unsubscribe();

        base.OnDisappearing();

    }
    protected override void OnSizeAllocated(double width, double height)
    {
        const double margin = 16 + 44 + 16 + 16;
        base.OnSizeAllocated(width, height);
        AlbumImage.WidthRequest = width - margin;
        AlbumImage.HeightRequest = width - margin;
    }
}
