﻿using Blu4Net;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

[QueryProperty(nameof(CurrentSong), nameof(CurrentSong))]
public partial class QueueViewModel : BaseRefreshViewModel, IAsyncDisposable
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    private bool _isGettingMore = false;
    private const int _numberOfItemsPerPage = 25;
    private IAsyncEnumerator<IReadOnlyCollection<PlayQueueSong>>? _iterator;

    public ObservableCollection<PlayQueueSong> Songs { get; } = [];

    [ObservableProperty]
    public partial int CurrentSong { get; set; }

    [ObservableProperty]
    public partial PlayQueueSong SelectedItem { get; set; }

    partial void OnSelectedItemChanged(PlayQueueSong value)
    {
        Debug.WriteLine("Selected:" + value);
        if (value != null && !IsBusy && CurrentSong != value.ID)
        {
            var t = _bluPlayerService.BluPlayer!.Play(value.ID);
            var x = t.Result;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Title = AppResources.Loading;
        // https://dev.to/dotnet/infinite-scrolling-with-incremental-data-loading-in-xamarin-forms-18b5
        try
        {
            var currentIndex = CurrentSong;
            var pageIndex = (int)(currentIndex / (double)_numberOfItemsPerPage);

            if (_bluPlayerService.IsConnected is false)
            {
                await _noConnectionDialogService.ShowAsync();
                return;
            }

            Songs.Clear();
            var asyncPages = _bluPlayerService.BluPlayer.PlayQueue.GetSongs(_numberOfItemsPerPage);
            _iterator = asyncPages.GetAsyncEnumerator();

            var currentPage = -1;
            while (await _iterator.MoveNextAsync() && currentPage < pageIndex)
            {
                currentPage++;
                if (currentPage < pageIndex)
                {
                    Debug.WriteLine($"Page {currentPage} is not the page I am searching for ({pageIndex}). Next!");
                    continue;
                }

                var songs = _iterator.Current;
                var i = currentPage * _numberOfItemsPerPage;
                foreach (var song in songs)
                {
                    Songs.Add(song);

                    if (currentIndex == i)
                    {
                        SelectedItem = song;
                    }
                    i++;
                }
            }

            Title = AppResources.Queue;
        }
        catch (Exception exception)
        {
            Title = AppResources.NoQueue;
            Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisplayActionSheetAsync(PlayQueueSong entry)
    {
        Debug.WriteLine(entry.Title);

        var menu = new Dictionary<MenuAction, string>
        {
            //{ MenuAction.TrackStation, AppResources.PlayTrackstation },
            //{ MenuAction.SimilarStation, AppResources.PlaySimilarstation },
            { MenuAction.GoToAlbum, AppResources.BrowseAlbum },
            { MenuAction.GoToArtist, AppResources.BrowseArtist },
            //{ MenuAction.AddToFavorites, AppResources.AddToFavorites },
            { MenuAction.RemoveFromList, AppResources.RemoveFromList }
        };

        var page = Shell.Current.CurrentPage;

        var action = await page.DisplayActionSheet(AppResources.Actions, AppResources.Cancel, null, menu.Select(e => e.Value).ToArray());

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
                    await Shell.Current.Navigation.PopToRootAsync(false); //navigate back to player
                    await Shell.Current.GoToAsync($"//browse?{nameof(entry.Service)}={entry.Service}&{nameof(entry.AlbumID)}={entry.AlbumID}");
                    break;
                case MenuAction.GoToArtist:
                    await Shell.Current.Navigation.PopToRootAsync(false); //navigate back to player
                    await Shell.Current.GoToAsync($"//browse?{nameof(entry.Service)}={entry.Service}&{nameof(entry.ArtistID)}={entry.ArtistID}");
                    break;
                case MenuAction.AddToFavorites:
                    break;
                case MenuAction.RemoveFromList:
                    await RemoveFromListAsync(entry.ID);
                    break;
                default:
                    break;
            }

            Debug.WriteLine($"Action clicked: {actionEntry.Key} {entry.ID}");
        }
    }

    [RelayCommand]
    private async Task LoadMoreDataAsync()
    {
        if (_isGettingMore || IsBusy)
        {
            return;
        }

        if (_iterator == null)
        {
            return;
        }

        try
        {
            _isGettingMore = true;

            if (await _iterator.MoveNextAsync())
            {
                var songs = _iterator.Current;
                Debug.WriteLine($"Yup getting {songs.Count} more songs!");
                foreach (var song in songs)
                {
                    Songs.Add(song);
                }
            }
            else
            {
                await _iterator.DisposeAsync();
                _iterator = null;
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

    private async Task RemoveFromListAsync(int id)
    {
        await _bluPlayerService.BluPlayer!.PlayQueue.Remove(id);
        IsBusy = true;
    }

    public async ValueTask DisposeAsync()
    {
        Songs.Clear();
        if (_iterator is not null)
        {
            await _iterator.DisposeAsync();
        }
    }
}
