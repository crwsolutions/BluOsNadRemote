namespace BluOsNadRemote.App.Views;

public partial class BrowsePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    internal BrowseViewModel ViewModel => (BrowseViewModel)BindingContext;    

    partial void PreConstruct() => InitializeComponent();

    protected async override void OnAppearing()
    {
        //If navigated to here from another page, QueryProperties are not set when you remove the next line:
        await Task.Yield(); //https://github.com/xamarin/Xamarin.Forms/issues/11549
        base.OnAppearing();
    }

    //Handle device backbutton
    protected override bool OnBackButtonPressed()
    {
        if (ViewModel.HasParent && ViewModel.GoBackCommand.CanExecute(null))
        {
            ViewModel.GoBackCommand.Execute(null);

            return true;
        }

        return base.OnBackButtonPressed();
    }
}
