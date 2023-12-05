﻿namespace BluOsNadRemote.App.Views;

public partial class PlayerPage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private PlayerViewModel ViewModel => BindingContext as PlayerViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected override void OnSizeAllocated(double width, double height)
    {
        var min = Math.Min(width, height);
        base.OnSizeAllocated(width, height);
        const double margin = 6 + 44 + 16 + 6;
        AlbumImage.WidthRequest = min - margin;
        AlbumImage.MaximumWidthRequest = min - margin;
        AlbumImage.HeightRequest = min - margin;
        AlbumImage.MaximumHeightRequest = min - margin;
    }
}
