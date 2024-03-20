using Blu4Net;
using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Utils;

namespace BluOsNadRemote.App.Services;

public sealed partial class BluPlayerService
{
    [Dependency]
    private readonly EndpointRepository _endpointRepository;

    public bool IsConnected { get; set; }

    public async Task<BluPlayerConnectResult> ConnectAsync()
    {
        if (_endpointRepository.SelectedEndpoint == null)
        {
            return new BluPlayerConnectResult(AppResources.NoConnection, false);
        }

        try
        {
            var uri = _endpointRepository.SelectedEndpoint.Uri;
            BluPlayer = await BluPlayer.Connect(uri);
            Debug.WriteLine($"Player: {BluPlayer}");
        }
        catch (Exception exception)
        {
            return new BluPlayerConnectResult(AppResources.CouldNotConnectResult.Interpolate(exception.Message), false);
        }

#if DEBUG            
        BluPlayer.Log = new DebugTextWriter();
#endif
        IsConnected = true;

        return new BluPlayerConnectResult(BluPlayer.ToString(), true);
    }

    public void Disconnect()
    {
        BluPlayer = null;
        IsConnected = false;
    }

    public async Task<BluPlayerDiscoverResult> DiscoverAsync()
    {
        var timeout = TimeSpan.FromSeconds(5);
        var protocol = "_musc._tcp.local.";
        var services = await ZeroConfTemp.ZeroconfResolver.ResolveAsync(protocol, timeout);

        if (services == null || services.Count == 0)
        {
            services = await ZeroConfTemp.ZeroconfResolver.ResolveAsync(protocol, timeout);
        }

        if (services == null || services.Count == 0)
        {
            services = await ZeroConfTemp.ZeroconfResolver.ResolveAsync(protocol, timeout);
        }

        if (services == null || services.Count == 0)
        {
            return new BluPlayerDiscoverResult(AppResources.DiscoverNoPlayersFound, false);
        }

        var endpoints = new EndPoint[services.Count];
        for (var i = 0; i < services.Count; i++)
        {
            var service = services[i];
            var bluPlayer = await BluPlayer.Connect(service.IPAddress);
            endpoints[i] = new EndPoint($"http://{service.IPAddress}:{BluEnvironment.DefaultEndpointPort}/", bluPlayer.Name);
        }

        _endpointRepository.MergeEndpoints(endpoints);

        var connectResult = await ConnectAsync();

        return new BluPlayerDiscoverResult(AppResources.DiscoverPlayersFound.Interpolate(endpoints.Length), true);
    }

    public BluPlayer BluPlayer { get; private set; }

    public MusicContentEntry MusicContentEntry { get; set; }
    public MusicContentNode MusicContentNode { get; set; }
}