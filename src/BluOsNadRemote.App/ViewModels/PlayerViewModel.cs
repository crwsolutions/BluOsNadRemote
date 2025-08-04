using Blu4Net;
using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class PlayerViewModel : BaseRefreshViewModel, IDisposable
{
    private string? _skipAction;
    private string? _backAction;

    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    private IDisposable? _volumeChangesSubscriber;
    private IDisposable? _stateChangesSubscriber;
    private IDisposable? _mediaChangesSubscriber;
    private IDisposable? _positionChangesSubscriber;
    private IDisposable? _shuffleChangesSubscriber;
    private IDisposable? _repeatChangesSubscriber;
    private int? _currentSong;
    private IDispatcherTimer? _timer;

    public bool IsSeeking { get; set; }

    internal void EstimateProgress()
    {
        if (IsSeeking) return;
        if (PlayerState == PlayerState.Streaming || PlayerState == PlayerState.Playing)
        {
            Elapsed = DateTime.Now - _startTimestamp;
            UpdateProgress();
        }
    }

    [ObservableProperty]
    public partial double Progress { get; set; } = 0;

    [ObservableProperty]
    public partial TimeSpan Length { get; set; }

    [ObservableProperty]
    public partial TimeSpan Elapsed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MuteImage))]
    [NotifyPropertyChangedFor(nameof(IsMuted))]
    [NotifyPropertyChangedFor(nameof(VolumeSymbol))]
    public partial int Volume { get; set; } = 0;

    public string VolumeSymbol => Volume switch
    {
        0 => "g",
        < 5 => "f",
        < 40 => "1",
        < 75 => "2",
        _ => "3"
    };

    partial void OnVolumeChanging(int value)
    {
        _ = SetAndClampVolumeAsync(value);
    }

    public string State => AppResources.ResourceManager.GetString(PlayerState.ToString(), AppResources.Culture)!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(State))]
    [NotifyPropertyChangedFor(nameof(IsPlaying))]
    public partial PlayerState PlayerState { get; set; }

    public bool IsPlaying => PlayerState == PlayerState.Streaming || PlayerState == PlayerState.Playing;

    partial void OnPlayerStateChanged(PlayerState value) => SetControls();

    [ObservableProperty]
    public partial string StreamFormat { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityImageIcon))]
    [NotifyPropertyChangedFor(nameof(QualityKbs))]
    [NotifyPropertyChangedFor(nameof(QualityKbsVisible))]
    public partial string? Quality { get; set; }

    partial void OnQualityChanged(string? value)
    {
        _qualityKbs = null;
        if (int.TryParse(value, out var kbs))
        {
            _qualityKbs = kbs;
        }
    }

    public string QualityImageIcon => _qualityKbs != null || Quality == null ? $"none_{ThemePostfix}" : $"{Quality}_{ThemePostfix}";

    private string ThemePostfix => Application.Current!.UserAppTheme == AppTheme.Dark ? "white" : "black";

    private int? _qualityKbs;

    public string? QualityKbs => _qualityKbs != null ? $"{_qualityKbs / 1000} kb/s" : null;

    public bool QualityKbsVisible => _qualityKbs != null;

    public string MuteImage => IsMuted || Volume == 0 ? "f" : "g";

    [ObservableProperty]
    public partial bool IsMuted { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPauseVisible))]
    public partial bool IsStartVisible { get; set; }

    [ObservableProperty]
    public partial bool IsBackVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSkipVisible { get; set; }

    public bool IsPauseVisible => !IsStartVisible;

    [ObservableProperty]
    public partial string Title1 { get; set; }

    [ObservableProperty]
    public partial string Title2 { get; set; }

    [ObservableProperty]
    public partial string Title3 { get; set; }

    [ObservableProperty]
    public partial Uri? MediaImageUri { get; set; }

    [ObservableProperty]
    public partial Uri? ServiceIconUri { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShuffleModeColor))]
    public partial ShuffleMode ShuffleMode { get; set; }

    public Color ShuffleModeColor => GetOnOffColor(ShuffleMode == ShuffleMode.ShuffleOff);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RepeatModeColor))]
    [NotifyPropertyChangedFor(nameof(RepeatModeSymbol))]
    public partial RepeatMode RepeatMode { get; set; } = RepeatMode.RepeatOff;

    public Color RepeatModeColor => GetOnOffColor(RepeatMode == RepeatMode.RepeatOff);

    private static Color GetOnOffColor(bool isOff) => isOff ? Colors.Gray : Application.Current!.UserAppTheme == AppTheme.Dark ? Colors.White : Colors.Black;

    public string RepeatModeSymbol => RepeatMode == RepeatMode.RepeatOne ? "s" : "d";

    public int? SongID { get; set; }

    public string Service { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreMenu))]
    public partial string ArtistID { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreMenu))]
    public partial string AlbumID { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreMenu))]
    public partial string TrackstationID { get; set; }

    public bool HasMoreMenu => ArtistID != null || AlbumID != null || TrackstationID != null;

    [ObservableProperty]
    public partial bool CanSeek { get; set; }

    [RelayCommand]
    private async Task SeekToPositionAsync(double progress)
    {
        if (!CanSeek || Length.Ticks == 0)
        {
            return;
        }
        var seekTicks = (long)(progress * Length.Ticks);
        var seekTime = new TimeSpan(seekTicks);
        if (_bluPlayerService.BluPlayer != null)
        {
            await _bluPlayerService.BluPlayer.Seek(seekTime);
            _startTimestamp = DateTime.Now - seekTime;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Title = AppResources.Loading;
        PlayerState = PlayerState.Unknown;

        try
        {
            // yes, so create and connect the player
            var result = await _bluPlayerService.ConnectAsync();
            Title = result.Message;
            if (result.HasConnections == false)
            {
                await _noConnectionDialogService.ShowHasNoConnectionsAsync();
                return;
            }

            if (_bluPlayerService.IsConnected == false)
            {
                await _noConnectionDialogService.ShowAsync();
                return;
            }

            var bluPlayer = _bluPlayerService.BluPlayer;

            Dispose();

            // subscribe to volume changes
            _volumeChangesSubscriber = bluPlayer.VolumeChanges.Subscribe(volume =>
            {
                Debug.WriteLine($"Volume: {volume}%");
                Volume = volume;
            });

            _stateChangesSubscriber = bluPlayer.StateChanges.Subscribe(UpdatePlayerState);

            _repeatChangesSubscriber = bluPlayer.RepeatModeChanges.Subscribe(repeatMode =>
            {
                Debug.WriteLine($"RepeatMode: {repeatMode}");
                RepeatMode = repeatMode;
            });

            _shuffleChangesSubscriber = bluPlayer.ShuffleModeChanges.Subscribe(shuffleMode =>
            {
                Debug.WriteLine($"ShuffleMode: {shuffleMode}");
                ShuffleMode = shuffleMode;
            });

            _mediaChangesSubscriber = bluPlayer.MediaChanges.Subscribe(UpdatePlayerMedia);

            _positionChangesSubscriber = bluPlayer.PositionChanges.Subscribe(UpdatePlayPosition);

            Title = bluPlayer.ToString();

            // get the state
            var state = await bluPlayer.GetState();
            UpdatePlayerState(state);

            // get the volume
            var playerVolume = await bluPlayer.GetVolume();
            Volume = playerVolume;

            // get the current playing media
            var media = await bluPlayer.GetMedia();
            UpdatePlayerMedia(media);

            var position = await bluPlayer.GetPosition();
            UpdatePlayPosition(position);

            ShuffleMode = await bluPlayer.GetShuffleMode();
            RepeatMode = await bluPlayer.GetRepeatMode();

            Debug.WriteLine($"Media: {media.Titles.FirstOrDefault()}");

            if (_timer == null)
            {
                _timer = Application.Current!.Dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(100);
                _timer.Tick += (s, e) => EstimateProgress();
            }

            if (!_timer.IsRunning)
            {
                _timer.Start();
            }
        }
        catch (Exception exception)
        {
            Title = AppResources.NoConnect;
            Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private DateTime _startTimestamp = DateTime.Now;

    private void UpdatePlayPosition(PlayPosition position)
    {
        if (IsSeeking) return;
        Debug.WriteLine($"Elapsed: {position.Elapsed}, Position: {position.Length}");
        _startTimestamp = DateTime.Now - position.Elapsed;
        if (position.Elapsed.Ticks <= position.Length?.Ticks)
        {
            Elapsed = position.Elapsed;
            Length = position.Length.Value;
            UpdateProgress();
        }
        else
        {
            Progress = 1;
            Elapsed = position.Elapsed;
            Length = new TimeSpan(0, 0, 0);
        }
    }

    private void UpdateProgress()
    {
        Progress = Elapsed.Ticks / (double)Length.Ticks;
    }

    private void UpdatePlayerState(PlayerState playerState)
    {
        Debug.WriteLine($"PlayerState: {playerState}");
        _startTimestamp = DateTime.Now - Elapsed;
        IsStartVisible = playerState.ToPlayerCanBeStarted();
        PlayerState = playerState;
    }

    private void UpdatePlayerMedia(PlayerMedia media)
    {
        Title1 = media.Titles.Count > 0 ? media.Titles[0] : string.Empty;
        Title2 = media.Titles.Count > 1 ? media.Titles[1] : string.Empty;
        Title3 = media.Titles.Count > 2 ? media.Titles[2] : string.Empty;
        _currentSong = media.Song;
        MediaImageUri = media.ImageUri;
        ServiceIconUri = media.ServiceIconUri;
        Quality = media.Quality;
        StreamFormat = media.StreamFormat;

        Service = media.ServiceName;
        SongID = media.Song;
        ArtistID = media.ArtistID;
        AlbumID = media.AlbumID;
        TrackstationID = media.TrackstationID;

        SetActions(media.Actions);
        PlayerState = media.PlayerState;
        CanSeek = media.CanSeek;
    }

    private void SetActions(IReadOnlyList<StreamingRadioAction> actions)
    {
        _backAction = null;
        _skipAction = null;

        if (actions == null)
        {
            return;
        }

        foreach (var action in actions)
        {
            if (action.Action == PlayerAction.Back && action.Url != null)
            {
                _backAction = action.Url;
            }
            if (action.Action == PlayerAction.Skip && action.Url != null)
            {
                _skipAction = action.Url;
            }
        }
    }

    private void SetControls()
    {
        if (PlayerState == PlayerState.Streaming)
        {
            var backButtonVisibility = false;
            var skipButtonVisibility = false;
            if (_backAction != null)
            {
                backButtonVisibility = true;
            }
            if (_skipAction != null)
            {
                skipButtonVisibility = true;
            }
            IsBackVisible = backButtonVisibility;
            IsSkipVisible = skipButtonVisibility;
        }
        else if (PlayerState == PlayerState.Playing)
        {
            IsBackVisible = true;
            IsSkipVisible = true;
        }
    }

    private async Task SetAndClampVolumeAsync(int percentage)
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            await _bluPlayerService.BluPlayer.SetVolume(Math.Clamp(percentage, 0, 100));
        }
    }

    public void Dispose()
    {
        _mediaChangesSubscriber?.Dispose();
        _mediaChangesSubscriber = null;
        _positionChangesSubscriber?.Dispose();
        _positionChangesSubscriber = null;
        _stateChangesSubscriber?.Dispose();
        _stateChangesSubscriber = null;
        _volumeChangesSubscriber?.Dispose();
        _volumeChangesSubscriber = null;
        _shuffleChangesSubscriber?.Dispose();
        _shuffleChangesSubscriber = null;
        _repeatChangesSubscriber?.Dispose();
        _repeatChangesSubscriber = null;
        MediaImageUri = null;
        ServiceIconUri = null;
        Quality = null;
        _timer?.Stop();
        _timer = null;
    }

    [RelayCommand]
    private async Task ToggleMuteAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            var isMuted = await _bluPlayerService.BluPlayer.Mute(!IsMuted);
            IsMuted = isMuted == 1;
        }
    }

    [RelayCommand]
    private async Task VolumeUpAsync() => await SetAndClampVolumeAsync(Volume + 2);

    [RelayCommand]
    private async Task VolumeDownAsync() => await SetAndClampVolumeAsync(Volume - 2);

    [RelayCommand]
    private async Task StopAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            PlayerState = await _bluPlayerService.BluPlayer.Stop();
        }
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            PlayerState = await _bluPlayerService.BluPlayer.Play();
        }
    }

    [RelayCommand]
    private async Task PauseAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            PlayerState = await _bluPlayerService.BluPlayer.Pause();
        }
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            if (_skipAction == null)
            {
                await _bluPlayerService.BluPlayer.Skip();
            }
            else
            {
                await _bluPlayerService.BluPlayer.Action(_skipAction);
            }
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            if (_backAction == null)
            {
                await _bluPlayerService.BluPlayer.Back();
            }
            else
            {
                await _bluPlayerService.BluPlayer.Action(_backAction);
            }
        }
    }

    [RelayCommand]
    private async Task ToggleShuffleAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            ShuffleMode = ShuffleMode.ToNextShuffleMode();
            await _bluPlayerService.BluPlayer.SetShuffleMode(ShuffleMode);
        }
    }

    [RelayCommand]
    private async Task ToggleRepeatAsync()
    {
        if (_bluPlayerService.BluPlayer != null)
        {
            RepeatMode = RepeatMode.ToNextRepeatMode();
            await _bluPlayerService.BluPlayer.SetRepeatMode(RepeatMode);
        }
    }

    [RelayCommand]
    private async Task NavigateToQueueAsync()
    {
        var song = PlayerState == PlayerState.Streaming ? -1 : _currentSong;

        await Shell.Current.GoToAsync($"{nameof(QueuePage)}?{nameof(QueueViewModel.CurrentSong)}={song}");
    }

    [RelayCommand]
    private async Task DisplayActionSheetAsync()
    {
        Debug.WriteLine(Title);

        var menu = new Dictionary<MenuAction, string>();
        if (AlbumID != null)
        {
            menu.Add(MenuAction.GoToAlbum, AppResources.BrowseAlbum);
        }
        if (ArtistID != null)
        {
            menu.Add(MenuAction.GoToArtist, AppResources.BrowseArtist);
        }

        var page = Shell.Current.CurrentPage;

        var action = await page.DisplayActionSheet(AppResources.Actions, AppResources.Cancel, null, menu.Select(e => e.Value).ToArray());

        var actionEntry = menu.FirstOrDefault(e => e.Value == action);

        if (!actionEntry.Equals(default(KeyValuePair<MenuAction, string>)))
        {
            switch (actionEntry.Key)
            {
                case MenuAction.GoToAlbum:
                    await Shell.Current.GoToAsync($"//browse?{nameof(Service)}={Service}&{nameof(AlbumID)}={AlbumID}");
                    break;
                case MenuAction.GoToArtist:
                    await Shell.Current.GoToAsync($"//browse?{nameof(Service)}={Service}&{nameof(ArtistID)}={ArtistID}");
                    break;
                default:
                    break;
            }

            Debug.WriteLine($"Action clicked: {actionEntry.Key} {SongID}");
        }
    }
}