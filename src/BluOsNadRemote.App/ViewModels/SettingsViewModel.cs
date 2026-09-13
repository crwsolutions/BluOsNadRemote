using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class SettingsViewModel : BaseRefreshViewModel, IDisposable, IQueryAttributable
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [Dependency]
    private readonly EndpointRepository _endpointRepository;

    [ObservableProperty]
    public partial bool IsDiscovering { get; set; } = false;

    [ObservableProperty]
    public partial string? Result { get; set; } = "";

    [ObservableProperty]
    public partial EndPoint? SelectedItem { get; set; }

    public string Version => $"{AppInfo.Current.Name} [v{AppInfo.Current.VersionString}] build {AppInfo.Current.BuildString}";

    partial void OnSelectedItemChanged(EndPoint? value)
    {
        if (value is not null)
        { 
            _endpointRepository.SelectedEndpoint = value; //When there are items, there is always a selected item.
        }
        _bluPlayerService.Disconnect();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_noConnectionDialogService.HasHandledDiscover)
        {
            return;
        }

        if (query.TryGetValue("discover", out var value) && value?.ToString() == "true")
        {
            _noConnectionDialogService.HasHandledDiscover = true;
            _ = DiscoverAsync();
        }
    }

    public override void IsLoading()
    {
        try
        {
            EndPoints.Clear();

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
    private async Task DeleteAsync(EndPoint? endpoint)
    {
        if (endpoint is null)
        {
            return;
        }

        var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
            AppResources.DeletePlayerConfirmTitle,
            AppResources.DeletePlayerConfirmMessage.Interpolate(endpoint.LastKnowName),
            AppResources.Yes,
            AppResources.No);

        if (confirmed is false)
        {
            return;
        }

        _bluPlayerService.Disconnect();
        _endpointRepository.RemoveEndPoint(endpoint);
        IsLoading();
    }

    [RelayCommand]
    private async Task DiscoverAsync()
    {
        try
        {
            // Disconnect only; the stored endpoints are kept and merged by the discovery, so
            // players that are not on the network right now are not lost.
            _bluPlayerService.Disconnect();
            IsDiscovering = true;
            var discoverResult = await _bluPlayerService.DiscoverAsync();
            Debug.WriteLine(discoverResult.Message);
            Result = discoverResult.Message;
            if (discoverResult.HasDiscovered)
            {
                // Fall back to the first endpoint if the previously selected one is no longer available.
                if (_endpointRepository.SelectedEndpoint is not null
                    && _endpointRepository.GetEndPoints().FirstOrDefault(e => e.Uri == _endpointRepository.SelectedEndpoint!.Uri) is null)
                {
                    _endpointRepository.SelectedEndpoint = _endpointRepository.GetEndPoints()[0];
                }

                // Rebuild the list (old + new, merged) and reconnect with the (possibly new) selection.
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
