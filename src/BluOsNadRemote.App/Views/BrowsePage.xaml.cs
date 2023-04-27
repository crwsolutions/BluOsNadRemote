namespace BluOsNadRemote.App.Views;

public partial class BrowsePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private BrowseViewModel ViewModel => BindingContext as BrowseViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected async override void OnAppearing()
    {
        //If navigated to here from another page, QueryProperties are not set when you remove the next line:
        await Task.Yield(); //https://github.com/xamarin/Xamarin.Forms/issues/11549
        base.OnAppearing();
    }

    protected override bool OnBackButtonPressed()
    {
        if (ViewModel.IsSearching && ViewModel.HideSearchCommand.CanExecute(null))
        {
            ViewModel.HideSearchCommand.Execute(null);
        }

        if (ViewModel.HasParent && ViewModel.GoBackCommand.CanExecute(null))
        {
            ViewModel.GoBackCommand.Execute(null);

            return true;
        }

        return base.OnBackButtonPressed();
    }

    private async void More_Clicked(object sender, EventArgs e)
    {
        var btn = (BindableObject)sender;
        var entry = (MusicContentEntryViewModel)btn.BindingContext;

        Debug.WriteLine(entry.Entry.Name);

        var contextMenu = await entry.Entry.ResolveContextMenu();

        Debug.WriteLine(contextMenu.Entries.Count);
        var options = contextMenu.Entries.Select(t => t.Name).ToArray();

        string action = await DisplayActionSheet("Actions", "Cancel", null, options);

        var actionEntry = contextMenu.Entries.FirstOrDefault(e => e.Name == action);

        Debug.WriteLine("Action clicked: " + actionEntry?.ActionURL);

        if (actionEntry == null)
        {
            return;
        }

        ViewModel.ActionURL = actionEntry.ActionURL;
        ViewModel.IsBusy = true;
    }
}
