namespace BluOsNadRemote.App.Views;

public partial class BrowsePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    internal BrowseViewModel ViewModel => BindingContext as BrowseViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected async override void OnAppearing()
    {
        //If navigated to here from another page, QueryProperties are not set when you remove the next line:
        await Task.Yield(); //https://github.com/xamarin/Xamarin.Forms/issues/11549
        base.OnAppearing();
    }
}
