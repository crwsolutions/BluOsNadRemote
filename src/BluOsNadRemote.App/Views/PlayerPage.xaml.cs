namespace BluOsNadRemote.App.Views;

public partial class PlayerPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private PlayerViewModel ViewModel => BindingContext as PlayerViewModel;

    partial void PreConstruct() => InitializeComponent();

    volatile bool run;

    protected override void OnAppearing()
    {
        base.OnAppearing();

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
    }

    protected override void OnDisappearing()
    {
        run = false;
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
