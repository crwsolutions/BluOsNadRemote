using Blu4Net;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Repositories;
using Nad4Net;

namespace BluOsNadRemote.App.ViewModels;

public partial class SettingsPlayerViewModel : BaseRefreshViewModel
{
    [Dependency]
    private readonly EndpointRepository _endpointRepository;

    [ObservableProperty]
    public partial string Result { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Uri))]
    public partial string Host { get; set; }

    public string Uri => $"http://{Host}:{BluEnvironment.DefaultEndpointPort}/";

    [RelayCommand]
    private async Task TelnetPingAsync()
    {
        try
        {
            Result = string.Empty;
            IsBusy = true;
            var uri = new Uri(Uri);
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
        try
        {
            Result = string.Empty;
            IsBusy = true;
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync($"{Uri}Status");
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
    private async Task SaveAsync()
    {
        try
        {
            Result = string.Empty;
            IsBusy = true;
            var uri = new Uri(Uri, UriKind.RelativeOrAbsolute);
            var bluPlayer = await BluPlayer.Connect(uri);
            var endPoint = new EndPoint(Uri, bluPlayer.Name);
            _endpointRepository.MergeEndpoints([endPoint]);
            await Shell.Current.GoToAsync("..");
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
}
