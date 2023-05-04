using Blu4Net;
using BluOsNadRemote.App.Services;
using Nad4Net;

namespace BluOsNadRemote.App.ViewModels;

public partial class SettingsViewModel : BaseRefreshViewModel
{
    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    [ObservableProperty]
    private string _result = "";

    public string Endpoint => Preferences.Endpoint;

    public string Host
    {
        get
        {
            if (Preferences.Endpoint == null)
            {
                return null;
            }

            try
            {
                var uri = new Uri(Preferences.Endpoint);
                return uri.Host;

            }
            catch (Exception ex)
            {
                Result = ex.Message;
                return string.Empty;
            }
        }
        set
        {
            string uri = null;
            if (value != null)
            {
                uri = $"http://{value}:{BluEnvironment.DefaultEndpointPort}/";
            }
            if (Preferences.Endpoint != uri)
            {
                Preferences.Endpoint = uri;
                OnPropertyChanged(nameof(Endpoint));
            }
        }
    }

    [RelayCommand]
    private async Task TelnetPingAsync()
    {
        try
        {
            var uri = new Uri(Preferences.Endpoint);
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
