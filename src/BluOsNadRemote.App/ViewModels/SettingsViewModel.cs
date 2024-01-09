using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class SettingsViewModel : BaseRefreshViewModel
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [Dependency]
    private readonly ConfigurationService _configurationService;

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
            SelectedItem = _configurationService.SelectedEndpoint;
            OnPropertyChanged(nameof(EndPoints));
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
            IsBusy = true;
            Reset();
            var discoverResult = await _bluPlayerService.DiscoverAsync();
            Result = discoverResult.Message;
            if (discoverResult.HasDiscovered)
            {
                Debug.WriteLine("HasSuccesfully discovered");
                SelectedItem = _configurationService.SelectedEndpoint;
                _ = await _bluPlayerService.ConnectAsync();
            }
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
        Result = null;
        _bluPlayerService.IsConnected = false;
        _configurationService.Clear();
        EndPoints.Clear();
    }
}
