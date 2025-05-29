using Blu4Net;
using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

[QueryProperty(nameof(Service), nameof(Service))]
[QueryProperty(nameof(AlbumID), nameof(AlbumID))]
[QueryProperty(nameof(ArtistID), nameof(ArtistID))]

public partial class BrowseViewModel : BaseRefreshViewModel, IDisposable
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    private string? _playURL;
    private string? _searchTerm;
    private bool? _isGettingMore;
    private MusicContentNode? _moreNode;

    public string? Service { get; set; } // = navigation parameter
    public string? AlbumID { get; set; } // = navigation parameter
    public string? ArtistID { get; set; } // = navigation parameter

    [ObservableProperty]
    public partial string SearchParameter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoHome))]
    public partial bool HasParent { get; set; } = false;

    public bool IsNotSearching => !IsSearching;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSearching))]
    [NotifyPropertyChangedFor(nameof(CanGoHome))]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    public partial bool IsSearching { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    public partial bool IsSearchable { get; set; } = false;

    [ObservableProperty]
    public partial bool CollectionIsVisible { get; set; } = true;

    public bool CanGoHome => !IsSearching && HasParent;
    public bool CanSearch => IsSearchable && !IsSearching;

    [ObservableProperty]
    public partial Uri? ServiceIconUri { get; set; }
    public ObservableCollection<MusicContentCategoryViewModel> Categories { get; } = [];

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            Title = AppResources.Loading;

            if (_bluPlayerService.IsConnected == false)
            {
                var result = await _bluPlayerService.ConnectAsync();
                Title = result.Message;
            }

            if (_bluPlayerService.IsConnected == false)
            {
                await _noConnectionDialogService.ShowAsync();
                return;
            }

            if (_playURL != null)
            {
                await _bluPlayerService.BluPlayer.MusicBrowser.PlayURL(_playURL);
                await Shell.Current.GoToAsync($"//{nameof(PlayerPage)}");
                return;
            }
            else if (_searchTerm != null)
            {
                var searchNode = await _bluPlayerService.MusicContentNode.Search(_searchTerm);
                _bluPlayerService.MusicContentNode = searchNode;
            }
            else if (Service != null)
            {
                MusicContentNode node;
                if (AlbumID != null)
                {
                    node = await _bluPlayerService.BluPlayer.MusicBrowser.GetNodeAlbumNode(Service, AlbumID);
                }
                else
                {
                    node = await _bluPlayerService.BluPlayer.MusicBrowser.GetNodeArtistNode(Service, ArtistID);
                }
                _bluPlayerService.MusicContentNode = node;
            }
            else if (_bluPlayerService.MusicContentEntry?.IsResolvable == true)
            {
                var musicContentNode = await _bluPlayerService.MusicContentEntry.Resolve();
                _bluPlayerService.MusicContentEntry = null;
                _bluPlayerService.MusicContentNode = musicContentNode;
            }
            else
            {
                _bluPlayerService.MusicContentNode ??= _bluPlayerService.BluPlayer.MusicBrowser;
            }
            HasParent = _bluPlayerService.MusicContentNode.Parent != null;
            IsSearchable = _bluPlayerService.MusicContentNode.IsSearchable;

            if (IsSearching)
            {
                IsSearching = IsSearchable;
            }

            Dispose();
            
            var incomingList = new List<MusicContentCategoryViewModel>();
            foreach (var category in _bluPlayerService.MusicContentNode.Categories)
            {
                incomingList.Add(new MusicContentCategoryViewModel(category, _bluPlayerService));
            }
            if (_bluPlayerService.MusicContentNode.Entries.Count != 0)
            {
                incomingList.Add(new MusicContentCategoryViewModel(_bluPlayerService.MusicContentNode.HasNext, _bluPlayerService.MusicContentNode.Entries, _bluPlayerService));

                if (_bluPlayerService.MusicContentNode.HasNext)
                {
                    _moreNode = _bluPlayerService.MusicContentNode;
                }
            }
#if ANDROID
            //Hack: Fusing does not work in Android, but just clearing and adding does not work in iOs :(.
            Categories.Clear();
            Debug.WriteLine($"**** Cleared...");
#endif

#if WINDOWS //#HACK: https://github.com/dotnet/maui/issues/18481
            CollectionIsVisible = false;
#endif

            Categories.Fuse(incomingList);

#if WINDOWS //#HACK: https://github.com/dotnet/maui/issues/18481
            CollectionIsVisible = true;
#endif


            Title = _bluPlayerService.MusicContentNode?.ServiceName ?? AppResources.AvailableServices;
            ServiceIconUri = _bluPlayerService.MusicContentNode?.ServiceIconUri;
        }
        catch (Exception exception)
        {
            Title = AppResources.NoBrowsers;
            _bluPlayerService.Disconnect();
            Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
            _playURL = null;
            _searchTerm = null;
            Service = null;
            AlbumID = null;
            ArtistID = null;
        }
    }

    [RelayCommand]
    private void Search()
    {
        _searchTerm = SearchParameter;
        IsBusy = true;
    }

    [RelayCommand]
    private async Task GetMoreItemsAsync()
    {
        if (_isGettingMore is true || IsBusy)
        {
            return;
        }

        if (_moreNode?.HasNext == true)
        {
            try
            {
                _isGettingMore = true;

                var node = await _bluPlayerService.MusicContentNode!.ResolveNext();

                Debug.WriteLine($"Loading {node.Entries.Count} more items !!!");

                var lastCategory = Categories.Last();
                lastCategory.AddRange(node.Entries.Select(e => new MusicContentEntryViewModel(e, _bluPlayerService)));

                if (node.HasNext)
                {
                    _moreNode = node;
                }
                else
                {
                    _moreNode = null;
                }
            }
            catch (Exception exception)
            {
                Title = AppResources.NoMore;
                Debug.WriteLine(exception);
            }
            finally
            {
                _isGettingMore = false;
            }
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _bluPlayerService.MusicContentNode = _bluPlayerService.MusicContentNode!.Parent;
        IsBusy = true;
    }

    [RelayCommand]
    private void GoHome()
    {
        _bluPlayerService.MusicContentNode = null;
        IsBusy = true;
    }

    [RelayCommand]
    private void ShowSearch()
    {
        IsSearching = true;
    }

    [RelayCommand]
    private void HideSearch()
    {
        IsSearching = false;
    }

    [RelayCommand]
    private void PresetTapped(MusicContentEntryViewModel item)
    {
        //await Shell.Current.GoToAsync($"{nameof(MusicBrowserPage)}");

        if (item.Entry.IsResolvable)
        {
            _bluPlayerService.MusicContentEntry = item.Entry;
            IsBusy = true;
            return;
        }

        if (item.Entry.AutoplayURL != null && item.Entry.Type != "Track")
        {
            _playURL = item.Entry.AutoplayURL;
            IsBusy = true;
        }

        if (item.Entry.PlayURL != null)
        {
            _playURL = item.Entry.PlayURL;
            IsBusy = true;
        }
    }

    [RelayCommand]
    private void Play(MusicContentEntry entry)
    {
        _playURL = entry.PlayURL;
        IsBusy = true;
    }

    public void Dispose()
    {
        _moreNode = null;
    }
}
