namespace BluOsNadRemote.App.Views;

public partial class PlayerPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private PlayerViewModel ViewModel => BindingContext as PlayerViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        const double margin = 16 + 44 + 16 + 16;
        AlbumImage.WidthRequest = width - margin;
        AlbumImage.MaximumWidthRequest = width - margin;
        AlbumImage.HeightRequest = width - margin;
        AlbumImage.MaximumHeightRequest = width - margin;
    }
}
