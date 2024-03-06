using BluOsNadRemote.App.Resources.Localizations;
using BluOsNadRemote.App.Services;
using Nad4Net.Model;

namespace BluOsNadRemote.App.ViewModels;

public partial class AdvancedViewModel : BaseRefreshViewModel, IDisposable
{
    [Dependency]
    private readonly NadTelnetService _service;

    private IDisposable _commandChangesSubscriber;
    private bool _isReceiving = false;

    [RelayCommand]
    private async Task ToggleOnOffAsync() => await _service.NadRemote.ToggleOnOffAsync();

    [RelayCommand(AllowConcurrentExecutions = true)] //Bug: https://github.com/CommunityToolkit/dotnet/issues/150#issuecomment-1069660045
    private async Task LoadDataAsync()
    {
        Title = AppResources.Loading;

        try
        {
            var result = _service.Connect();
            if (result.IsConnected == false)
            {
                Title = result.Message;
                return;
            }

            await _service.NadRemote.GetCommandListAsync(UpdateCommandlist);
            _commandChangesSubscriber = _service.NadRemote.CommandChanges.Subscribe(UpdateCommandlist);
        }
        catch (InvalidOperationException exception)
        {
            Title = AppResources.NoConnect;
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

    public string[] ListeningModes => ["None", "NeuralX", "EnhancedStereo", "DolbySurround", "EARS"];

    [ObservableProperty]
    private string _mainSource;
    partial void OnMainSourceChanging(string value)
    {
        _ = _service.NadRemote.ForceSourceNameUpdateAsync(value);
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
            _ = _service.NadRemote.SetListeningModeAsync(value);
        }
    }

    [ObservableProperty]
    private int _mainDirac = -1;

    partial void OnMainDiracChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            _ = _service.NadRemote?.SetMainDiracAsync(value);
        }
    }

    [ObservableProperty]
    private string _dirac1State;

    [ObservableProperty]
    private string _dirac1Name;
    partial void OnDirac1NameChanging(string value)
    {
        if (!Diracs.Any(d => d == value))
        {
            Diracs.Add(value);
        }
    }

    [ObservableProperty]
    private string _dirac2State;

    [ObservableProperty]
    private string _dirac2Name;
    partial void OnDirac2NameChanging(string value)
    {
        if (!Diracs.Any(d => d == value))
        {
            Diracs.Add(value);
        }
    }

    [ObservableProperty]
    private string _dirac3State;

    [ObservableProperty]
    private string _dirac3Name;
    partial void OnDirac3NameChanging(string value)
    {
        if (!Diracs.Any(d => d == value))
        {
            Diracs.Add(value);
        }
    }

    public ObservableCollection<string> Diracs { get; } = [];

    [ObservableProperty]
    private int _mainTrimSub;

    partial void OnMainTrimSubChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            if (MainTrimSub < value)
            {
                _ = _service.NadRemote.DoSubPlus();
            }

            if (MainTrimSub > value)
            {
                _ = _service.NadRemote.DoSubMinus();
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
                _ = _service.NadRemote.DoSurroundPlus();
            }

            if (MainTrimSurround > value)
            {
                _ = _service.NadRemote.DoSurroundMinus();
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
                _ = _service.NadRemote.DoCenterPlus();
            }

            if (MainTrimCenter > value)
            {
                _ = _service.NadRemote.DoCenterMinus();
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
            _ = _service.NadRemote.ToggleDimmerAsync();
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
            _service.Disconnect();
            MainDirac = -1;
            //MainSourceName = null;
            //MainSource = null;
            IsBusy = false;
            _isReceiving = false;
        }
        catch { };
    }
}
