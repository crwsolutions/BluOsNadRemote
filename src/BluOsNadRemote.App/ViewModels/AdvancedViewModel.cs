using BluOsNadRemote.App.Resources.Languages;
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
    public partial string MainSource { get; set; }

    [ObservableProperty]
    public partial string MainSourceName { get; set; }

    [ObservableProperty]
    public partial string MainAudioCODEC { get; set; }

    [ObservableProperty]
    public partial string MainAudioChannels { get; set; }

    [ObservableProperty]
    public partial string MainAudioRate { get; set; }

    [ObservableProperty]
    public partial string MainListeningMode { get; set; }

    partial void OnMainListeningModeChanging(string value)
    {
        if (!IsBusy && !_isReceiving)
        {
            _service.NadRemote.SetListeningModeAsync(value).Wait();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiracLabel))]
    public partial int MainDirac { get; set; } = -1;

    partial void OnMainDiracChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            _service.NadRemote?.SetMainDiracAsync(value).Wait();
        }
    }

    public string DiracLabel => $"{AppResources.Dirac} ({MainDirac})";

    [ObservableProperty]
    public partial string Dirac1State { get; set; }

    [ObservableProperty]
    public partial string Dirac1Name { get; set; }

    partial void OnDirac1NameChanging(string value)
    {
        if (!Diracs.Any(d => d == value))
        {
            Diracs.Add(value);
        }
    }

    [ObservableProperty]
    public partial string Dirac2State { get; set; }

    [ObservableProperty]
    public partial string Dirac2Name { get; set; }

    partial void OnDirac2NameChanging(string value)
    {
        if (!Diracs.Any(d => d == value))
        {
            Diracs.Add(value);
        }
    }

    [ObservableProperty]
    public partial string Dirac3State { get; set; }

    [ObservableProperty]
    public partial string Dirac3Name { get; set; }

    partial void OnDirac3NameChanging(string value)
    {
        if (!Diracs.Any(d => d == value))
        {
            Diracs.Add(value);
        }
    }

    public ObservableCollection<string> Diracs { get; } = [];

    [ObservableProperty]
    public partial int MainTrimSub { get; set; }

    partial void OnMainTrimSubChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            if (MainTrimSub < value)
            {
               _service.NadRemote.DoSubPlusAsync().Wait();
            }

            if (MainTrimSub > value)
            {
                _service.NadRemote.DoSubMinusAsync().Wait();
            }
        }
    }

    [ObservableProperty]
    public partial int MainTrimSurround { get; set; }

    partial void OnMainTrimSurroundChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            if (MainTrimSurround < value)
            {
                _service.NadRemote.DoSurroundPlusAsync().Wait();
            }

            if (MainTrimSurround > value)
            {
                _service.NadRemote.DoSurroundMinusAsync().Wait();
            }
        }
    }

    [ObservableProperty]
    public partial int MainTrimCenter { get; set; }

    partial void OnMainTrimCenterChanging(int value)
    {
        if (!IsBusy && !_isReceiving)
        {
            if (MainTrimCenter < value)
            {
                _service.NadRemote.DoCenterPlusAsync().Wait();
            }

            if (MainTrimCenter > value)
            {
                _service.NadRemote.DoCenterMinusAsync().Wait();
            }
        }
    }

    [ObservableProperty]
    public partial bool MainDimmer { get; set; }

    partial void OnMainDimmerChanged(bool value)
    {
        Debug.WriteLine($"Setting dimmer to {value}");
        if (!IsBusy && !_isReceiving)
        {
            _service.NadRemote.ToggleDimmerAsync().Wait();
        }
    }

    [ObservableProperty]
    public partial bool MainPower { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ARCColor))]
    public partial string MainVideoARC { get; set; }

    private static readonly Color disabledColor = new(53, 54, 54);
    public Color ARCColor => MainVideoARC?.ToLower() == "yes" ? Colors.Green : disabledColor;

    [ObservableProperty]
    public partial string MainDolbyDRC { get; set; }

    public void Dispose()
    {
        try
        {
            Debug.WriteLine("Disposing telnet shizzle");
            _commandChangesSubscriber?.Dispose();
            _commandChangesSubscriber = null;
            _service.Disconnect();
            MainDirac = -1;
            IsBusy = false;
            _isReceiving = false;
        }
        catch { };
    }
}
