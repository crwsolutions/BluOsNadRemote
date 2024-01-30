﻿using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class SettingsViewModel : BaseRefreshViewModel, IDisposable
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [Dependency]
    private readonly ConfigurationService _configurationService;

    [ObservableProperty]
    private bool _isDiscovering = false;

    [ObservableProperty]
    private string _result = "";

    [ObservableProperty]
    private EndPoint _selectedItem;

    partial void OnSelectedItemChanged(EndPoint value)
    {
        _configurationService.SelectedEndpoint = value;
    }

    public override void IsLoading()
    {
        try
        {
            var endPoints = _configurationService.GetEndPoints();
            if (endPoints is null || endPoints.Length == 0)
            {
                return;
            }

            EndPoints = new ObservableCollection<EndPoint>(endPoints);
            OnPropertyChanged(nameof(EndPoints));
            SelectedItem = _configurationService.SelectedEndpoint;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public ObservableCollection<EndPoint> EndPoints { get; set; }

    [RelayCommand]
    private async Task NavigateToAddAsync() => await Shell.Current.GoToAsync(nameof(SettingsPlayerPage));

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
        _bluPlayerService.IsConnected = false;
        _configurationService.Clear();
        Dispose();
    }

    public void Dispose()
    {
        Result = null;
        EndPoints?.Clear();
        SelectedItem = null;
    }
}
