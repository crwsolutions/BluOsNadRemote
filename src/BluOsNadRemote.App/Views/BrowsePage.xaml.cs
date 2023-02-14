namespace BluOsNadRemote.App.Views;

public partial class BrowsePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private BrowseViewModel ViewModel => BindingContext as BrowseViewModel;

    partial void PreConstruct() => InitializeComponent();
}
