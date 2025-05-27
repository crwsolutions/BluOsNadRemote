namespace BluOsNadRemote.App.Views;

public partial class PlayerPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private PlayerViewModel ViewModel => (PlayerViewModel)BindingContext;

    partial void PreConstruct() => InitializeComponent();

    protected override void OnSizeAllocated(double width, double height)
    {
        var min = Math.Min(width, height);
#if WINDOWS
        min = Math.Min(min, 540);
#endif
        base.OnSizeAllocated(width, height);
        const double margin = 8 + 8 + 58 + 8;
        AlbumImage.WidthRequest = min - margin;
        AlbumImage.MaximumWidthRequest = min - margin;
        AlbumImage.HeightRequest = min - margin;
        AlbumImage.MaximumHeightRequest = min - margin;
    }

    private void OnSeekSliderDragStarted(object sender, EventArgs e)
    {
        ViewModel.IsSeeking = true;
    }

    private async void OnSeekSliderDragCompleted(object sender, EventArgs e)
    {
        try
        {
            if (sender is Slider slider)
            {
                await ViewModel.SeekToPositionCommand.ExecuteAsync(slider.Value);
            }
        }
        finally
        {
                ViewModel.IsSeeking = false;
        }
    }
}
