using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;
using Nad4Net;
using Nad4Net.Model;

namespace BluOsNadRemote.App.ViewModels;

public partial class AdvancedViewModel : BaseRefreshViewModel, IDisposable
{
    [Dependency]
    private readonly NadTelnetService _service;

    private IDisposable? _commandChangesSubscriber;
    private bool _isReceiving = false;
    private bool _isLoading = false;
    private bool _disposed = false;

    [RelayCommand]
    private async Task ToggleOnOffAsync()
    {
        if (_service.NadRemote is null)
        {
            return;
        }

        try
        {
            await _service.NadRemote.ToggleOnOffAsync();
        }
        catch (Exception exception)
        {
            HandleControlFailure(exception);
        }
    }

    /// <summary>
    /// Runs a telnet control command from a synchronous property-changed handler without
    /// ever surfacing a raw exception to the UI.
    /// </summary>
    private void RunCommand(Func<Task> command)
    {
        if (_service.NadRemote is null)
        {
            return;
        }

        try
        {
            command().Wait();
        }
        catch (Exception exception)
        {
            HandleControlFailure(exception);
        }
    }

    /// <summary>
    /// A failed control command (silent disconnect, unreachable NAD) must not surface as a
    /// raw "technical error": show the friendly "could not connect" title instead. A swipe
    /// refresh reconnects.
    /// </summary>
    private void HandleControlFailure(Exception exception)
    {
        Debug.WriteLine($"NAD control command failed: {exception}");
        Title = AppResources.NoConnect;
    }

    [RelayCommand(AllowConcurrentExecutions = true)] // RefreshView sets IsBusy via the TwoWay binding before the command runs, so IsBusy can't be used as the guard; _isLoading is.
    private async Task LoadDataAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        _disposed = false;
        IsBusy = true;
        Title = AppResources.Loading;

        try
        {
            // Drop any subscription to a previous connection first, so the old change-detection
            // loop cannot push stale data into the new load.
            _commandChangesSubscriber?.Dispose();
            _commandChangesSubscriber = null;

            var result = await _service.ConnectAsync();
            if (result.IsConnected == false)
            {
                Title = AppResources.NoConnect;
                await _noConnectionDialogService.ShowAsync(
                    result.Reason == NadConnectReason.NoEndpoint
                        ? AppResources.NoEndpointMessage
                        : string.Format(AppResources.CouldNotConnectResult, result.Host ?? string.Empty));
                return;
            }

            if (_service.NadRemote != null)
            {
                await _service.NadRemote.GetCommandListAsync(UpdateCommandlist);
                _commandChangesSubscriber = _service.NadRemote.CommandChanges.Subscribe(UpdateCommandlist);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("LoadDataAsync was cancelled");
        }
        catch (Exception exception)
        {
            Title = AppResources.NoConnect;
            await _noConnectionDialogService.ShowAsync();
            Debug.WriteLine(exception);
        }
        finally
        {
            _isLoading = false;
            IsBusy = false;
        }
    }

    private void UpdateCommandlist(CommandList commandList)
    {
        if (_disposed)
        {
            return;
        }

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
    }

    public string[] ListeningModes => ["None", "NeuralX", "EnhancedStereo", "DolbySurround", "EARS"];

    [ObservableProperty]
    public partial string? MainSource { get; set; }

    [ObservableProperty]
    public partial string? MainSourceName { get; set; }

    [ObservableProperty]
    public partial string? MainAudioCODEC { get; set; }

    [ObservableProperty]
    public partial string? MainAudioChannels { get; set; }

    [ObservableProperty]
    public partial string? MainAudioRate { get; set; }

    [ObservableProperty]
    public partial string? MainListeningMode { get; set; }

    partial void OnMainListeningModeChanging(string? value)
    {
        if (!IsBusy && !_isReceiving && _service.NadRemote != null && value is not null)
        {
            RunCommand(() => _service.NadRemote.SetListeningModeAsync(value));
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiracLabel))]
    public partial int MainDirac { get; set; } = -1;

    partial void OnMainDiracChanging(int value)
    {
        if (!IsBusy && !_isReceiving && _service.NadRemote != null)
        {
            RunCommand(() => _service.NadRemote.SetMainDiracAsync(value));
        }
    }

    public string DiracLabel => $"{AppResources.Dirac} ({MainDirac})";

    [ObservableProperty]
    public partial string? Dirac1State { get; set; }

    [ObservableProperty]
    public partial string? Dirac1Name { get; set; }

    partial void OnDirac1NameChanging(string? value)
    {
        if (!Diracs.Any(d => d == value) && value is not null)
        {
            Diracs.Add(value);
        }
    }

    [ObservableProperty]
    public partial string? Dirac2State { get; set; }

    [ObservableProperty]
    public partial string? Dirac2Name { get; set; }

    partial void OnDirac2NameChanging(string? value)
    {
        if (!Diracs.Any(d => d == value) && value is not null)
        {
            Diracs.Add(value);
        }
    }

    [ObservableProperty]
    public partial string? Dirac3State { get; set; }

    [ObservableProperty]
    public partial string? Dirac3Name { get; set; }

    partial void OnDirac3NameChanging(string? value)
    {
        if (!Diracs.Any(d => d == value) && value is not null)
        {
            Diracs.Add(value);
        }
    }

    public ObservableCollection<string> Diracs { get; } = [];

    [ObservableProperty]
    public partial int MainTrimSub { get; set; }

    partial void OnMainTrimSubChanging(int value)
    {
        if (!IsBusy && !_isReceiving && _service.NadRemote != null)
        {
            if (MainTrimSub < value)
            {
                RunCommand(() => _service.NadRemote.DoSubPlusAsync());
            }

            if (MainTrimSub > value)
            {
                RunCommand(() => _service.NadRemote.DoSubMinusAsync());
            }
        }
    }

    [ObservableProperty]
    public partial int MainTrimSurround { get; set; }

    partial void OnMainTrimSurroundChanging(int value)
    {
        if (!IsBusy && !_isReceiving && _service.NadRemote != null)
        {
            if (MainTrimSurround < value)
            {
                RunCommand(() => _service.NadRemote.DoSurroundPlusAsync());
            }

            if (MainTrimSurround > value)
            {
                RunCommand(() => _service.NadRemote.DoSurroundMinusAsync());
            }
        }
    }

    [ObservableProperty]
    public partial int MainTrimCenter { get; set; }

    partial void OnMainTrimCenterChanging(int value)
    {
        if (!IsBusy && !_isReceiving && _service.NadRemote != null)
        {
            if (MainTrimCenter < value)
            {
                RunCommand(() => _service.NadRemote.DoCenterPlusAsync());
            }

            if (MainTrimCenter > value)
            {
                RunCommand(() => _service.NadRemote.DoCenterMinusAsync());
            }
        }
    }

    [ObservableProperty]
    public partial bool MainDimmer { get; set; }

    partial void OnMainDimmerChanged(bool value)
    {
        Debug.WriteLine($"Setting dimmer to {value}");
        if (!IsBusy && !_isReceiving && _service.NadRemote != null)
        {
            RunCommand(() => _service.NadRemote.ToggleDimmerAsync());
        }
    }

    [ObservableProperty]
    public partial bool MainPower { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ARCColor))]
    public partial string? MainVideoARC { get; set; }

    private static readonly Color disabledColor = new(53, 54, 54);
    public Color ARCColor => MainVideoARC?.ToLower() == "yes" ? Colors.Green : disabledColor;

    [ObservableProperty]
    public partial string? MainDolbyDRC { get; set; }

    public void Dispose()
    {
        try
        {
            Debug.WriteLine("Disposing telnet shizzle");
            _disposed = true;
            _commandChangesSubscriber?.Dispose();
            _commandChangesSubscriber = null;
            _service.Disconnect();
            MainDirac = -1;
            IsBusy = false;
            _isLoading = false;
            _isReceiving = false;
        }
        catch { };
    }
}
