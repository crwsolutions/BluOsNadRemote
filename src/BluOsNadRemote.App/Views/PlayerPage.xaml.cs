namespace BluOsNadRemote.App.Views;

public partial class PlayerPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private PlayerViewModel ViewModel => BindingContext as PlayerViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected override void OnSizeAllocated(double width, double height)
    {
        var min = Math.Min(width, height);
#if WINDOWS
        min = Math.Min(min, 540);
#endif
        base.OnSizeAllocated(width, height);
        const double margin = 8 + 66 + 8;
        AlbumImage.WidthRequest = min - margin;
        AlbumImage.MaximumWidthRequest = min - margin;
        AlbumImage.HeightRequest = min - margin;
        AlbumImage.MaximumHeightRequest = min - margin;
    }
}
