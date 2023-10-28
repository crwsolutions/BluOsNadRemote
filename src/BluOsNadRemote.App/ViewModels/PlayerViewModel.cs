using Blu4Net;
using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class PlayerViewModel : BaseRefreshViewModel, IDisposable
{
    private static readonly TimeSpan _estimateProgressTimespan = TimeSpan.FromSeconds(1);

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
        if (State == "Streaming" || State == "Playing")
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
    double _progress = 0;

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

    [ObservableProperty]
    private string _state;

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

    private string ThemePostfix => AppInfo.RequestedTheme == AppTheme.Dark ? "white" : "black";

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShuffleModeColor))]
    private ShuffleMode _shuffleMode;

    public Color ShuffleModeColor =>
                ShuffleMode == ShuffleMode.ShuffleOff
                ? Colors.Gray
                : Application.Current.UserAppTheme == AppTheme.Dark ? Colors.Black : Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RepeatModeColor))]
    [NotifyPropertyChangedFor(nameof(RepeatModeSymbol))]
    private RepeatMode _repeatMode = RepeatMode.RepeatOff;

    public Color RepeatModeColor =>
                RepeatMode == RepeatMode.RepeatOff
                ? Colors.Gray
                : Application.Current.UserAppTheme == AppTheme.Dark ? Colors.Black : Colors.White;

    public string RepeatModeSymbol => RepeatMode == RepeatMode.RepeatOne ? "s" : "d";

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

            Dispose();

            // subscribe to volume changes
            _volumeChangesSubscriber = bluPlayer.VolumeChanges.Subscribe(volume =>
            {
                Debug.WriteLine($"Volume: {volume}%");
                Volume = volume;
            });

            _stateChangesSubscriber = bluPlayer.StateChanges.Subscribe(playerState =>
            {
                Debug.WriteLine($"State: {playerState}");
                UpdatePlayerState(playerState);
                State = playerState.ToString();
            });

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
            var state2 = await bluPlayer.GetState();
            State = state2.ToString();
            Debug.WriteLine($"State: {state2}");
            UpdatePlayerState(state2);

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
            Title = "Connection failed";
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
        IsStartVisible = playerState.ToPlayerCanBeStarted();
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
        var song = State == nameof(PlayerState.Streaming) ? -1 : _currentSong;

        await Shell.Current.GoToAsync($"{nameof(QueuePage)}?{nameof(QueueViewModel.CurrentSong)}={song}");
    }
}
