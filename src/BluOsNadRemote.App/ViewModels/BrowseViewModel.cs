using Blu4Net;
using BluOsNadRemote.App.Extensions;
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
    private string _actionURL;
    private string _searchTerm;
    private bool _isGettingMore;
    private MusicContentNode _moreNode;

    public event EventHandler OnAfterListWasUpdated;

    public string Service { get; set; } // = navigation parameter
    public string AlbumID { get; set; } // = navigation parameter
    public string ArtistID { get; set; } // = navigation parameter

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private bool _hasParent = false;

    public bool IsNotSearching => !IsSearching;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasParent))]
    [NotifyPropertyChangedFor(nameof(IsSearchable))]
    [NotifyPropertyChangedFor(nameof(IsNotSearching))]
    private bool _isSearching = false;

    [ObservableProperty]
    private bool _isSearchable = false;

    [ObservableProperty]
    private Uri _serviceIconUri;

    public ObservableCollection<MusicContentCategoryViewModel> Categories { get; } = new ObservableCollection<MusicContentCategoryViewModel>();
    public ObservableCollection<MusicContentEntryViewModel> Entries { get; } = new ObservableCollection<MusicContentEntryViewModel>();
    public string ActionURL { set => _actionURL = value; }

    public bool HasEntries => Entries.Any();

    public bool HasCategories => Categories.Any();

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Title = "Loading...";

        try
        {
            if (_playURL != null)
            {
                await _bluPlayerService.BluPlayer.MusicBrowser.PlayURL(_playURL);
                await Shell.Current.GoToAsync($"//{nameof(PlayerPage)}");
                return;
            }
            if (_actionURL != null)
            {
                await _bluPlayerService.BluPlayer.MusicBrowser.PlayURL(_actionURL);
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
            foreach (var category in _bluPlayerService.MusicContentNode.Categories)
            {
                Categories.Add(new MusicContentCategoryViewModel(category));
            }
            if (_bluPlayerService.MusicContentNode.Entries.Any())
            {
                foreach (var entry in _bluPlayerService.MusicContentNode.Entries)
                {
                    //Debug.WriteLine($"**** Added: {entry.Name}");
                    Entries.Add(new MusicContentEntryViewModel(entry));
                }

                if (_bluPlayerService.MusicContentNode.HasNext)
                {
                    _moreNode = _bluPlayerService.MusicContentNode;
                }
            }
            OnPropertyChanged(nameof(HasCategories));
            OnPropertyChanged(nameof(HasEntries));
            OnAfterListWasUpdated?.Invoke(this, EventArgs.Empty);

            Title = _bluPlayerService.MusicContentNode?.ServiceName ?? "Available services";
            ServiceIconUri = _bluPlayerService.MusicContentNode?.ServiceIconUri;
        }
        catch (Exception exception)
        {
            Title = "Could not retrieve browsers";
            Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
            _playURL = null;
            _searchTerm = null;
            _actionURL = null;
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

                foreach (var entry in node.Entries)
                {
                    Debug.WriteLine($"**** Added extra: {entry.Name}");
                    Entries.Add(new MusicContentEntryViewModel(entry));
                }

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
                Title = "Could not retrieve more";
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
        Entries.Clear();
        _moreNode = null;
    }
}
