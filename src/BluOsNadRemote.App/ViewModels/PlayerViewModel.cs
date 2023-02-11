using Blu4Net;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class PlayerViewModel : BaseRefreshViewModel
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    private IDisposable _volumeChangesSubscriber;
    private IDisposable _stateChangesSubscriber;
    private IDisposable _mediaChangesSubscriber;
    private IDisposable _positionChangesSubscriber;
    private int? _currentSong;

    internal void UpdateProgress()
    {
        if (State == "Streaming" || State == "Playing")
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                // interact with UI elements
                Elapsed = Elapsed.Add(TimeSpan.FromSeconds(1));
                RecalculateProgress();
            });
        }
    }

    [ObservableProperty]
    double _progress = 0;

    [ObservableProperty]
    private TimeSpan _length;

    [ObservableProperty]
    private TimeSpan _elapsed;

    int _volume = 0;
    public int Volume
    {
        get { return _volume; }
        set
        {
            if (_volume != value)
            {
                _ = SetVolume(value);
            }
            if (SetProperty(ref _volume, value))
            {
                OnPropertyChanged(nameof(MuteImage));
                OnPropertyChanged(nameof(IsMuted));
            }
        }
    }

    [ObservableProperty]
    private string _state;


    private string _quality;
    public string Quality
    {
        get { return _quality; }
        set
        {
            if (SetProperty(ref _quality, value))
            {
                string image;

                if (int.TryParse(value, out var kbs))
                {
                    _qualityKbs = kbs;
                    image = "none";
                }
                else
                {
                    _qualityKbs = null;
                    image = _quality;
                }
                var postFix = AppInfo.RequestedTheme == AppTheme.Dark ? "white" : "black";
                _qualityImageIcon = $"{image}_{postFix}";

                OnPropertyChanged(nameof(QualityImageIcon));
                OnPropertyChanged(nameof(QualityKbs));
                OnPropertyChanged(nameof(QualityKbsVisible));
            }
        }
    }


    private string _qualityImageIcon;
    public string QualityImageIcon => _qualityImageIcon;

    private int? _qualityKbs;
    public string QualityKbs => _qualityKbs != null ? $"{_qualityKbs / 1000} kb/s" : null;

    public bool QualityKbsVisible => _qualityKbs != null;

    [ObservableProperty]
    private string _streamFormat;

    public string MuteImage => IsMuted || Volume == 0 ? "f" : "g";

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPauseVisible))]
    private bool _isStartVisible;

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

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Title = "Loading...";

        try
        {
            // yes, so create and connect the player
            Title = await _bluPlayerService.InitializeAsync();
            if (!_bluPlayerService.IsInitialized)
            {
                return;
            }

            var bluPlayer = _bluPlayerService.BluPlayer;

            Unsubscribe();

            // subscribe to volume changes
            _volumeChangesSubscriber = bluPlayer.VolumeChanges.Subscribe(volume =>
            {
                Debug.WriteLine($"Volume: {volume}%");
                Volume = volume;
            });

            _stateChangesSubscriber = bluPlayer.StateChanges.Subscribe(playerState =>
            {
                Debug.WriteLine($"State: {playerState}");
                SetState(playerState);
                State = playerState.ToString();
            });

            _mediaChangesSubscriber = bluPlayer.MediaChanges.Subscribe(SetMedia);

            _positionChangesSubscriber = bluPlayer.PositionChanges.Subscribe(SetPosition);

            Title = bluPlayer.ToString();

            // get the state
            var state2 = await bluPlayer.GetState();
            State = state2.ToString();
            Debug.WriteLine($"State: {state2}");
            SetState(state2);

            // get the volume
            var playerVolume = await bluPlayer.GetVolume();
            Volume = playerVolume;

            // get the current playing media
            var media = await bluPlayer.GetMedia();
            SetMedia(media);

            var position = await bluPlayer.GetPosition();
            SetPosition(position);

            Debug.WriteLine($"Media: {media.Titles.FirstOrDefault()}");

        }
        catch (Exception exception)
        {
            Title = "Connection failed";
            Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetPosition(PlayPosition position)
    {
        Debug.WriteLine($"Elapsed: {position.Elapsed}, Position: {position.Length}");

        if (position.Elapsed.Ticks <= position.Length?.Ticks)
        {
            Elapsed = position.Elapsed;
            Length = position.Length.Value;
            RecalculateProgress();
        }
        else
        {
            Progress = 1;
            Elapsed = position.Elapsed;
            Length = new TimeSpan(0, 0, 0);
        }
    }

    private void RecalculateProgress()
    {
        Progress = Elapsed.Ticks / (double)Length.Ticks;
    }

    private void SetState(PlayerState playerState)
    {
        if (playerState == PlayerState.Playing || playerState == PlayerState.Streaming)
        {
            IsStartVisible = false;
        }
        else
        {
            IsStartVisible = true;
        }
    }

    private void SetMedia(PlayerMedia media)
    {
        Title1 = media.Titles.Count > 0 ? media.Titles[0] : string.Empty;
        Title2 = media.Titles.Count > 1 ? media.Titles[1] : string.Empty;
        Title3 = media.Titles.Count > 2 ? media.Titles[2] : string.Empty;
        _currentSong = media.Song;
        MediaImageUri = media.ImageUri;
        ServiceIconUri = media.ServiceIconUri;
        Quality = media.Quality;
        StreamFormat = media.StreamFormat;
    }

    private async Task ToggleMute()
    {
        var isMUted = await _bluPlayerService.BluPlayer?.Mute(!IsMuted);
        IsMuted = isMUted == 1;
    }

    private async Task VolumeDown()
    {
        await SetVolume(Volume - 2);
    }

    private async Task SetVolume(int percentage)
    {
        if (percentage > 100)
            percentage = 100;

        if (percentage < 0)
            percentage = 0;

        await _bluPlayerService.BluPlayer.SetVolume(percentage);
    }

    private async Task VolumeUp()
    {
        await SetVolume(Volume + 2);
    }

    public void Unsubscribe()
    {
        _mediaChangesSubscriber?.Dispose();
        _mediaChangesSubscriber = null;
        _positionChangesSubscriber?.Dispose();
        _positionChangesSubscriber = null;
        _stateChangesSubscriber?.Dispose();
        _stateChangesSubscriber = null;
        _volumeChangesSubscriber?.Dispose();
        _volumeChangesSubscriber = null;
    }

    [RelayCommand]
    private async Task ToggleMuteAsync() => await ToggleMute();

    [RelayCommand]
    private async Task VolumeUpAsync() => await VolumeUp();

    [RelayCommand]
    private async Task VolumeDownAsync() => await VolumeDown();

    [RelayCommand]
    private async Task StopAsync() => await _bluPlayerService.BluPlayer?.Stop();

    [RelayCommand]
    private async Task PlayAsync() => await _bluPlayerService.BluPlayer?.Play();

    [RelayCommand]
    private async Task PauseAsync() => await _bluPlayerService.BluPlayer?.Pause();

    [RelayCommand]
    private async Task SkipAsync() => await _bluPlayerService.BluPlayer?.Skip();

    [RelayCommand]
    private async Task BackAsync() => await _bluPlayerService.BluPlayer?.Back();

    [RelayCommand]
    private async Task ToggleShuffleAsync() => Debug.WriteLine("Toggle shuffle");

    [RelayCommand]
    private async Task ToggleRepeatAsync() => Debug.WriteLine("Toggle repeat");

    [RelayCommand]
    private async Task NavigateToQueueAsync()
    {
        //if (State == nameof(PlayerState.Streaming))
        //{
        //    await Shell.Current.GoToAsync(nameof(QueuePage));
        //}
        //else
        //{
        //    await Shell.Current.GoToAsync($"{nameof(QueuePage)}?{nameof(QueueViewModel.CurrentSong)}={_currentSong}");
        //}

        await Shell.Current.GoToAsync(nameof(QueuePage), true, new Dictionary<string, object>
            {
                { "CurrentSong", State == nameof(PlayerState.Streaming) ? null : _currentSong }
            });
    }
}
