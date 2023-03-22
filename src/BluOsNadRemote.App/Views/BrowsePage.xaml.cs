namespace BluOsNadRemote.App.Views;

public partial class BrowsePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private BrowseViewModel ViewModel => BindingContext as BrowseViewModel;

    partial void PreConstruct() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();

        //await Task.Yield(); //https://github.com/xamarin/Xamarin.Forms/issues/11549
        ViewModel.OnAfterListWasUpdated += ViewModel_OnAfterListWasUpdated;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.OnAfterListWasUpdated -= ViewModel_OnAfterListWasUpdated;
    }

    private void ViewModel_OnAfterListWasUpdated(object sender, EventArgs e)
    {
        svScrollView.ScrollToAsync(0, 0, false);
    }

    protected override bool OnBackButtonPressed()
    {
        if (ViewModel.IsSearching && ViewModel.HideSearchCommand.CanExecute(null))
        {
            ViewModel.HideSearchCommand.Execute(null);
        }

        if (ViewModel.HasParent && ViewModel.GoBackCommand.CanExecute(null))
        {
            //BluPlayerSingleton.MusicContentNode = BluPlayerSingleton.MusicContentNode.Parent;

            ViewModel.GoBackCommand.Execute(null);

            return true;
        }


        return base.OnBackButtonPressed();
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {

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
