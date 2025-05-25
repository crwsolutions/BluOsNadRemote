using Blu4Net;
using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class PlayerViewModel : BaseRefreshViewModel, IDisposable
{
    private static readonly TimeSpan _estimateProgressTimespan = TimeSpan.FromSeconds(1);

    private string _skipAction;
    private string _backAction;

    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    private IDisposable _volumeChangesSubscriber;
    private IDisposable _stateChangesSubscriber;
    private IDisposable _mediaChangesSubscriber;
    private IDisposable _positionChangesSubscriber;
    private IDisposable _shuffleChangesSubscriber;
    private IDisposable _repeatChangesSubscriber;
    private int? _currentSong;
    private IDispatcherTimer _timer;

    internal void EstimateProgress()
    {
        if (PlayerState == PlayerState.Streaming || PlayerState == PlayerState.Playing)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // interact with UI elements
                Elapsed = Elapsed.Add(_estimateProgressTimespan);
                UpdateProgress();
            });
        }
    }

    [ObservableProperty]
    private double _progress = 0;

    [ObservableProperty]
    private TimeSpan _length;

    [ObservableProperty]
    private TimeSpan _elapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MuteImage))]
    [NotifyPropertyChangedFor(nameof(IsMuted))]
    int _volume = 0;

    partial void OnVolumeChanging(int value)
    {
        _ = SetAndClampVolumeAsync(value);
    }

    public string State => AppResources.ResourceManager.GetString(PlayerState.ToString(), AppResources.Culture);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(State))]
    [NotifyPropertyChangedFor(nameof(IsPlaying))]
    private PlayerState _playerState;

    public bool IsPlaying => PlayerState == PlayerState.Streaming || PlayerState == PlayerState.Playing;

    partial void OnPlayerStateChanged(PlayerState value) => SetControls();

    [ObservableProperty]
    private string _streamFormat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityImageIcon))]
    [NotifyPropertyChangedFor(nameof(QualityKbs))]
    [NotifyPropertyChangedFor(nameof(QualityKbsVisible))]
    private string _quality;

    partial void OnQualityChanged(string value)
    {
        _qualityKbs = null;
        if (int.TryParse(value, out var kbs))
        {
            _qualityKbs = kbs;
        }
    }

    public string QualityImageIcon => _qualityKbs != null || Quality == null ? $"none_{ThemePostfix}" : $"{Quality}_{ThemePostfix}";

    private string ThemePostfix => Application.Current.UserAppTheme == AppTheme.Dark ? "white" : "black";

    private int? _qualityKbs;
    public string QualityKbs => _qualityKbs != null ? $"{_qualityKbs / 1000} kb/s" : null;

    public bool QualityKbsVisible => _qualityKbs != null;

    public string MuteImage => IsMuted || Volume == 0 ? "f" : "g";

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPauseVisible))]
    private bool _isStartVisible;

    [ObservableProperty]
    private bool _isBackVisible;

    [ObservableProperty]
    private bool _isSkipVisible;

    public bool IsPauseVisible => !IsStartVisible;

    [ObservableProperty]
    private string _title1;

    [ObservableProperty]
    private string _title2;

    [ObservableProperty]
    private string _title3;

    [ObservableProperty]
    private Uri _mediaImageUri;

    [ObservableProperty]
    private Uri _serviceIconUri;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShuffleModeColor))]
    private ShuffleMode _shuffleMode;

    public Color ShuffleModeColor => GetOnOffColor(ShuffleMode == ShuffleMode.ShuffleOff);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RepeatModeColor))]
    [NotifyPropertyChangedFor(nameof(RepeatModeSymbol))]
    private RepeatMode _repeatMode = RepeatMode.RepeatOff;

    public Color RepeatModeColor => GetOnOffColor(RepeatMode == RepeatMode.RepeatOff);

    private static Color GetOnOffColor(bool isOff) => isOff ? Colors.Gray : Application.Current.UserAppTheme == AppTheme.Dark ? Colors.White : Colors.Black;

    public string RepeatModeSymbol => RepeatMode == RepeatMode.RepeatOne ? "s" : "d";

    public int? SongID { get; set; }

    public string Service { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreMenu))]
    private string _artistID;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreMenu))]

    private string _albumID;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreMenu))]
    private string _trackstationID;

    public bool HasMoreMenu => ArtistID != null || AlbumID != null || TrackstationID != null;

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
            if (result.IsConnected == false)
            {
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
                _timer = Application.Current.Dispatcher.CreateTimer();
                _timer.Interval = _estimateProgressTimespan;
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

    private void UpdatePlayPosition(PlayPosition position)
    {
        Debug.WriteLine($"Elapsed: {position.Elapsed}, Position: {position.Length}");

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
        await _bluPlayerService.BluPlayer.SetVolume(Math.Clamp(percentage, 0, 100));
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
        var isMUted = await _bluPlayerService.BluPlayer?.Mute(!IsMuted);
        IsMuted = isMUted == 1;
    }

    [RelayCommand]
    private async Task VolumeUpAsync() => await SetAndClampVolumeAsync(Volume + 2);

    [RelayCommand]
    private async Task VolumeDownAsync() => await SetAndClampVolumeAsync(Volume - 2);

    [RelayCommand]
    private async Task StopAsync() => PlayerState = await _bluPlayerService.BluPlayer?.Stop();

    [RelayCommand]
    private async Task PlayAsync() => PlayerState = await _bluPlayerService.BluPlayer?.Play();

    [RelayCommand]
    private async Task PauseAsync() => PlayerState = await _bluPlayerService.BluPlayer?.Pause();

    [RelayCommand]
    private async Task SkipAsync()
    {
        if (_skipAction == null)
        {
            await _bluPlayerService.BluPlayer?.Skip();
        }
        else
        {
            await _bluPlayerService.BluPlayer?.Action(_skipAction);
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (_backAction == null)
        {
            await _bluPlayerService.BluPlayer?.Back();
        }
        else
        {
            await _bluPlayerService.BluPlayer?.Action(_backAction);
        }
    }

    [RelayCommand]
    private async Task ToggleShuffleAsync()
    {
        ShuffleMode = ShuffleMode.ToNextShuffleMode();
        await _bluPlayerService.BluPlayer.SetShuffleMode(ShuffleMode);
    }

    [RelayCommand]
    private async Task ToggleRepeatAsync()
    {
        RepeatMode = RepeatMode.ToNextRepeatMode();
        await _bluPlayerService.BluPlayer.SetRepeatMode(RepeatMode);
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