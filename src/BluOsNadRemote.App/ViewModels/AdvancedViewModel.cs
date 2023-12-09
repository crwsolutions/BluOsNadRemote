using Nad4Net.Model;
using Nad4Net;

namespace BluOsNadRemote.App.ViewModels;

public partial class AdvancedViewModel : BaseRefreshViewModel, IDisposable
{
    private NadRemote _nadRemote;
    private IDisposable _commandChangesSubscriber;
    private bool _isReceiving = false;

    [RelayCommand]
    private async Task ToggleOnOffAsync()
    {
        await _nadRemote.ToggleOnOffAsync();
    }

    [RelayCommand(AllowConcurrentExecutions = true)] //Bug: https://github.com/CommunityToolkit/dotnet/issues/150#issuecomment-1069660045
    private async Task LoadDataAsync()
    {
        Title = "Loading...";

        if (Preferences.Endpoint == null)
        {
            Title = "There is no endpoint. Go to settings";
        }

        Uri uri;
        try
        {
            uri = new Uri(Preferences.Endpoint);
        }
        catch (Exception)
        {
            Title = "Unable to determine endpoint. Go to settings";
            return;
        }

        if (_nadRemote != null)
        {
            Dispose();
        }

        try
        {
            _nadRemote = new NadRemote(uri);

            var commandList = await _nadRemote.GetCommandListAsync();
            UpdateCommandlist(commandList);
            _commandChangesSubscriber = _nadRemote.CommandChanges.Subscribe(UpdateCommandlist);
            IsBusy = false;

        }
        catch (InvalidOperationException exception)
        {
            Title = $"No connect to: {uri}";
            Debug.WriteLine(exception);
        }
        catch (Exception exception)
        {
            Title = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCommandlist(CommandList commandList)
    {
        //Device.BeginInvokeOnMainThread(() =>
        //{
            _isReceiving = true;
            Title = commandList.MainModel;
            MainSource = commandList.MainSource;
            MainAudioCODEC = commandList.MainAudioCODEC;
            MainAudioChannels = commandList.MainAudioChannels;
            MainAudioRate = commandList.MainAudioRate;
            MainVideoARC = commandList.MainVideoARC;
            MainListeningMode = commandList.MainListeningMode;
            Dirac1State = commandList.Dirac1State;
            Dirac1Name = commandList.Dirac1Name;
            Dirac2State = commandList.Dirac2State;
            Dirac2Name = commandList.Dirac2Name;
            Dirac3State = commandList.Dirac3State;
            Dirac3Name = commandList.Dirac3Name;
            MainDirac = commandList.MainDirac;
            MainTrimSub = commandList.MainTrimSub;
            MainTrimSurround = commandList.MainTrimSurround;
            MainTrimCenter = commandList.MainTrimCenter;
            MainDimmer = commandList.MainDimmer;
            MainPower = commandList.MainPower;
            MainDolbyDRC = commandList.MainDolbyDRC;
            MainSourceName = commandList.MainSourceName;
            _isReceiving = false;
        //});
    }

    public string[] ListeningModes => new[] { "None", "NeuralX", "EnhancedStereo", "DolbySurround", "EARS" };

    [ObservableProperty]
    private string _mainSource;
    partial void OnMainSourceChanging(string value)
    {
        _ = _nadRemote.ForceSourceNameUpdateAsync(value);
    }

    [ObservableProperty]
    private string _mainSourceName;

    [ObservableProperty]
    private string _mainAudioCODEC;

    [ObservableProperty]
    private string _mainAudioChannels;

    [ObservableProperty]
    private string _mainAudioRate;

    [ObservableProperty]
    private string _mainListeningMode;
    partial void OnMainListeningModeChanging(string value)
    {
        if (!IsBusy && !_isReceiving)
        {
            _ = _nadRemote.SetListeningModeAsync(value);
        }
    }

    [ObservableProperty]
    private int _mainDirac = -1;

    partial void OnMainDiracChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            _ = _nadRemote?.SetMainDiracAsync(value);
        }
    }

    [ObservableProperty]
    private string _dirac1State;

    [ObservableProperty]
    private string _dirac1Name;
    partial void OnDirac1NameChanging(string value)
    {
        if (!_diracs.Any(d => d == value))
        {
            _diracs.Add(value);
        }
    }

    [ObservableProperty]
    private string _dirac2State;

    [ObservableProperty]
    private string _dirac2Name;
    partial void OnDirac2NameChanging(string value)
    {
        if (!_diracs.Any(d => d == value))
        {
            _diracs.Add(value);
        }
    }

    [ObservableProperty]
    private string _dirac3State;

    [ObservableProperty]
    private string _dirac3Name;
    partial void OnDirac3NameChanging(string value)
    {
        if (!_diracs.Any(d => d == value))
        {
            _diracs.Add(value);
        }
    }

    private readonly ObservableCollection<string> _diracs = new();
    public ObservableCollection<string> Diracs => _diracs;

    [ObservableProperty]
    private int _mainTrimSub;

    partial void OnMainTrimSubChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            if (MainTrimSub < value)
            {
                _ = _nadRemote.DoSubPlus();
            }
            if (MainTrimSub > value)
            {
                _ = _nadRemote.DoSubMinus();
            }
        }
    }

    [ObservableProperty]
    private int _mainTrimSurround;
    partial void OnMainTrimSurroundChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            if (MainTrimSurround < value)
            {
                _ = _nadRemote.DoSurroundPlus();
            }
            if (MainTrimSurround > value)
            {
                _ = _nadRemote.DoSurroundMinus();
            }
        }
    }

    [ObservableProperty]
    private int _mainTrimCenter;

    partial void OnMainTrimCenterChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            if (MainTrimCenter < value)
            {
                _ = _nadRemote.DoCenterPlus();
            }
            if (MainTrimCenter > value)
            {
                _ = _nadRemote.DoCenterMinus();
            }
        }
    }

    [ObservableProperty]
    private bool _mainDimmer;
    partial void OnMainDimmerChanged(bool value)
    {
        Debug.WriteLine($"Setting dimmer to {value}");
        if (!IsBusy && !_isReceiving)
        {
            _ = _nadRemote.ToggleDimmerAsync();
        }
    }

    [ObservableProperty]
    private bool _mainPower;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMainVideoARC))]
    private string _mainVideoARC;

    public bool IsMainVideoARC => MainVideoARC?.ToLower() == "yes";

    [ObservableProperty]
    private string _mainDolbyDRC;


    public void Dispose()
    {
        try
        {
            Debug.WriteLine("Disposing telnet shizzle");
            _commandChangesSubscriber?.Dispose();
            _commandChangesSubscriber = null;
            _nadRemote?.Dispose();
            _nadRemote = null;
            MainDirac = -1;
            MainSourceName = null;
            IsBusy = false;
            _isReceiving = false;
        }
        catch { };
    }
}
