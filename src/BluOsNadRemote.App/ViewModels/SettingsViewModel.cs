using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class SettingsViewModel : BaseRefreshViewModel, IDisposable
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [Dependency]
    private readonly EndpointRepository _endpointRepository;

    [ObservableProperty]
    public partial bool IsDiscovering { get; set; } = false;

    [ObservableProperty]
    public partial string Result { get; set; } = "";

    [ObservableProperty]
    public partial EndPoint SelectedItem { get; set; }

    public string Version => $"{AppInfo.Current.Name} [v{AppInfo.Current.VersionString}] build {AppInfo.Current.BuildString}";

    partial void OnSelectedItemChanged(EndPoint value)
    {
        _endpointRepository.SelectedEndpoint = value;
    }

    public override void IsLoading()
    {
        try
        {
            var endPoints = _endpointRepository.GetEndPoints();
            if (endPoints is null || endPoints.Length == 0)
            {
                return;
            }

            foreach (EndPoint endPoint in endPoints) 
            {
                EndPoints.Add(endPoint);
            }
            SelectedItem = _endpointRepository.SelectedEndpoint;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public ObservableCollection<EndPoint> EndPoints { get; } = [];

    [RelayCommand]
    private async Task NavigateToAddAsync() => await Shell.Current.GoToAsync(nameof(SettingsPlayerPage));

    [RelayCommand]
    private async Task NavigateToMoreAsync() => await Shell.Current.GoToAsync(nameof(SettingsMorePage));


    [RelayCommand]
    private async Task DiscoverAsync()
    {
        try
        {
            Reset();
            IsDiscovering = true;
            var discoverResult = await _bluPlayerService.DiscoverAsync();
            Debug.WriteLine(discoverResult.Message);
            Result = discoverResult.Message;
            if (discoverResult.HasDiscovered)
            {
                IsLoading();
                _ = await _bluPlayerService.ConnectAsync();
            }
        }
        catch (Exception exception)
        {
            Result = exception.ToString();
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        _bluPlayerService.Disconnect();
        _endpointRepository.ClearEndpoints();
        Dispose();
    }

    public void Dispose()
    {
        Result = null;
        EndPoints?.Clear();
        SelectedItem = null;
    }
}
