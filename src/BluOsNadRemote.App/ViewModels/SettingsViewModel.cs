﻿using Blu4Net;
using BluOsNadRemote.App.Services;
using Nad4Net;

namespace BluOsNadRemote.App.ViewModels;

public partial class SettingsViewModel : BaseRefreshViewModel
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [Dependency]
    private readonly PreferencesRepository _preferencesRepository;

    [ObservableProperty]
    private string _result = "";

    public string Endpoint => _preferencesRepository.SelectedEndpoint?.Uri?.ToString();

    public string Host
    {
        get
        {
            return _preferencesRepository.SelectedEndpoint?.Uri?.Host;
        }
        set
        {
            string uri = null;
            if (value != null)
            {
                uri = $"http://{value}:{BluEnvironment.DefaultEndpointPort}/";
            }
            if (_preferencesRepository.SelectedEndpoint.Uri?.ToString() != uri)
            {
                _preferencesRepository.SetEndpoint(uri);
                OnPropertyChanged(nameof(Endpoint));
            }
        }
    }

    [RelayCommand]
    private async Task TelnetPingAsync()
    {
        try
        {
            var uri = _preferencesRepository.SelectedEndpoint.Uri;
            var nadRemote = new NadRemote(uri);
            var result = await nadRemote.PingAsync();
            Result = $"Success {DateTime.Now}: {result}";
        }
        catch (Exception ex)
        {
            Result = $"Failed {DateTime.Now}: {ex}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BluOsPingAsync()
    {
        if (Endpoint == null)
        {
            Result = "No host, first discover";
            return;
        }

        try
        {
            Result = string.Empty;
            IsBusy = true;
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync($"{Endpoint}Status");
            string result;
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsStringAsync();
            }
            else
            {
                result = response.StatusCode.ToString();
            }
            Result = $"Success {DateTime.Now}: {result}";
        }
        catch (Exception ex)
        {
            Result = $"Failed {DateTime.Now}: {ex}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DiscoverAsync()
    {
        try
        {
            IsBusy = true;
            Reset();
            Result = await _bluPlayerService.InitializeAsync();
            OnPropertyChanged(nameof(Host));
            OnPropertyChanged(nameof(Endpoint));
        }
        catch (Exception exception)
        {
            Result = exception.ToString();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        Host = null;
        Result = null;
        _bluPlayerService.IsInitialized = false;
    }
}
