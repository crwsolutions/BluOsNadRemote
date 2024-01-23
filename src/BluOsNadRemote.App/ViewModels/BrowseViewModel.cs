using Blu4Net;
using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Resources.Localizations;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

[QueryProperty(nameof(Service), nameof(Service))]
[QueryProperty(nameof(AlbumID), nameof(AlbumID))]
[QueryProperty(nameof(ArtistID), nameof(ArtistID))]

public partial class BrowseViewModel : BaseRefreshViewModel, IDisposable
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    private string _playURL;
    private string _searchTerm;
    private bool _isGettingMore;
    private MusicContentNode _moreNode;

    public string Service { get; set; } // = navigation parameter
    public string AlbumID { get; set; } // = navigation parameter
    public string ArtistID { get; set; } // = navigation parameter

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoHome))]
    private bool _hasParent = false;

    public bool IsNotSearching => !IsSearching;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSearching))]
    [NotifyPropertyChangedFor(nameof(CanGoHome))]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private bool _isSearching = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private bool _isSearchable = false;

    public bool CanGoHome => !IsSearching && HasParent;
    public bool CanSearch => IsSearchable && !IsSearching;

    [ObservableProperty]
    private Uri _serviceIconUri;

    public ObservableCollection<MusicContentCategoryViewModel> Categories { get; } = [];

    [RelayCommand]
    private async Task LoadDataAsync()
    {

        try
        {
            Title = AppResources.Loading;

            var result = await _bluPlayerService.ConnectAsync();
            Title = result.Message;
            if (result.IsConnected == false)
            {
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

            Dispose();
            Debug.WriteLine($"**** Cleared...");
            //#if IOS
            //            await Task.Delay(300);  //HACK: //https://github.com/dotnet/maui/issues/10163
            //#endif
            foreach (var category in _bluPlayerService.MusicContentNode.Categories)
            {
                Categories.Add(new MusicContentCategoryViewModel(category, _bluPlayerService));
            }
            if (_bluPlayerService.MusicContentNode.Entries.Count != 0)
            {
                Categories.Add(new MusicContentCategoryViewModel(_bluPlayerService.MusicContentNode.HasNext, _bluPlayerService.MusicContentNode.Entries, _bluPlayerService));

                if (_bluPlayerService.MusicContentNode.HasNext)
                {
                    _moreNode = _bluPlayerService.MusicContentNode;
                }
            }

            Title = _bluPlayerService.MusicContentNode?.ServiceName ?? AppResources.AvailableServices;
            ServiceIconUri = _bluPlayerService.MusicContentNode?.ServiceIconUri;
        }
        catch (Exception exception)
        {
            Title = AppResources.NoBrowsers;
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
    private void Search(string searchTerm)
    {
        _searchTerm = searchTerm;
        IsBusy = true;
    }

    [RelayCommand]
    private async Task GetMoreItemsAsync()
    {
        //return;

        if (_isGettingMore || IsBusy)
        {
            return;
        }

        if (_moreNode?.HasNext == true)
        {
            try
            {
                _isGettingMore = true;

                var node = await _bluPlayerService.MusicContentNode.ResolveNext();

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
        _bluPlayerService.MusicContentNode = _bluPlayerService.MusicContentNode.Parent;
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
        Categories.Clear();
        _moreNode = null;
    }
}
