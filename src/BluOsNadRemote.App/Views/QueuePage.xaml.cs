using Blu4Net;

namespace BluOsNadRemote.App.Views;

public partial class QueuePage : BaseContentPage
{
    [Dependency(nameof(BindingContext))]
    private QueueViewModel ViewModel => BindingContext as QueueViewModel;

    partial void PreConstruct() => InitializeComponent();

    private async void ItemsListView_RemainingItemsThresholdReached(object sender, EventArgs e)
    {
        Debug.WriteLine("Get next data please!");
        await ViewModel.LoadMoreDataAsync();
    }

    enum MenuAction
    {
        TrackStation,
        SimilarStation,
        GoToAlbum,
        GoToArtist,
        AddToFavorites,
        RemoveFromList
    }

    private async void More_Clicked(object sender, EventArgs e)
    {
        var btn = (BindableObject)sender;
        var entry = (PlayQueueSong)btn.BindingContext;

        Debug.WriteLine(entry.Title);

        var menu = new Dictionary<MenuAction, string>
            {
                { MenuAction.TrackStation, "Play trackstation" },
                { MenuAction.SimilarStation, "Play similarstation" },
                { MenuAction.GoToAlbum, "Browse album" },
                { MenuAction.GoToArtist, "Browse artist" },
                { MenuAction.AddToFavorites, "Add to favorites" },
                { MenuAction.RemoveFromList, "Remove from list" }
            };

        var action = await DisplayActionSheet("Actions", "Cancel", null, menu.Select(e => e.Value).ToArray());

        var actionEntry = menu.FirstOrDefault(e => e.Value == action);

        if (!actionEntry.Equals(default(KeyValuePair<MenuAction, string>)))
        {
            switch (actionEntry.Key)
            {
                case MenuAction.TrackStation:
                    await Shell.Current.GoToAsync("..");
                    break;
                case MenuAction.SimilarStation:
                    await Shell.Current.GoToAsync("..");
                    break;
                case MenuAction.GoToAlbum:
                    //await Shell.Current.GoToAsync("..", false );
                    await Shell.Current.Navigation.PopToRootAsync(false);
                    //await Shell.Current.Navigation.PopAsync();
                    await Shell.Current.GoToAsync($"//{nameof(BrowsePage)}?{nameof(entry.Service)}={entry.Service}&{nameof(entry.AlbumID)}={entry.AlbumID}");
                    break;
                case MenuAction.GoToArtist:
                    //await Shell.Current.GoToAsync("..", false);
                    await Shell.Current.Navigation.PopToRootAsync(false);
                    //await Shell.Current.Navigation.PopAsync();
                    await Shell.Current.GoToAsync($"//{nameof(BrowsePage)}?{nameof(entry.Service)}={entry.Service}&{nameof(entry.ArtistID)}={entry.ArtistID}");
                    break;
                case MenuAction.AddToFavorites:
                    break;
                case MenuAction.RemoveFromList:
                    await ViewModel.RemoveFromListAsync(entry.ID);
                    break;
                default:
                    break;
            }

            Debug.WriteLine($"Action clicked: {actionEntry.Key} {entry.ID}");
        }
    }
}
