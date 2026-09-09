using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Utils;
using BluOsNadRemote.Blu4Net;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BluOsNadRemote.App.Services;

public sealed partial class BluPlayerService
{
    [Dependency]
    private readonly EndpointRepository _endpointRepository;

    [Dependency]
    private readonly LanguageService _languageService;

    partial void PostConstruct() => _languageService.LanguageObservable().Subscribe(UpdateAcceptLanguage);

    private void UpdateAcceptLanguage(CultureInfo info)
    {
        Debug.WriteLine($"Updating bluplayer language to '{info.Name}'");
        BluPlayer?.UpdateAcceptLanguage(info);
    }

    private bool _isConnected;

    [MemberNotNullWhen(true, nameof(BluPlayer))]
    public bool IsConnected => _isConnected && BluPlayer is not null;

    public async Task<BluPlayerConnectResult> ConnectAsync()
    {
        if (_endpointRepository.GetEndPoints().Length == 0)
        {
            return new BluPlayerConnectResult(AppResources.NoConnections, false, false);
        }

        if (_endpointRepository.SelectedEndpoint == null)
        {
            return new BluPlayerConnectResult(AppResources.NoConnection, false, true);
        }

        try
        {
            var uri = _endpointRepository.SelectedEndpoint.Uri;
            BluPlayer = await BluPlayer.Connect(uri, AppResources.Culture);
            Debug.WriteLine($"Player: {BluPlayer}");
        }
        catch (Exception exception)
        {
            return new BluPlayerConnectResult(AppResources.CouldNotConnectResult.Interpolate(exception.Message), false, true);
        }

#if DEBUG            
        BluPlayer.Log = new DebugTextWriter();
#endif
        _isConnected = true;

        return new BluPlayerConnectResult(BluPlayer.ToString(), true, true);
    }

    public void Disconnect()
    {
        BluPlayer = null;
        _isConnected = false;
    }

    public async Task<BluPlayerDiscoverResult> DiscoverAsync()
    {
        var endpoints = await BluEnvironment.ResolveEndpointsAsync(AppResources.Culture);

        if (endpoints.Count == 0)
        {
            return new BluPlayerDiscoverResult(AppResources.DiscoverNoPlayersFound, false);
        }

        _endpointRepository.MergeEndpoints(
            [.. endpoints.Select(endpoint => new EndPoint(endpoint.Uri, endpoint.Name))]);

        var connectResult = await ConnectAsync();

        return new BluPlayerDiscoverResult(AppResources.DiscoverPlayersFound.Interpolate(endpoints.Count), true);
    }

    public BluPlayer? BluPlayer { get; private set; }

    public MusicContentEntry? MusicContentEntry { get; set; }

    public MusicContentNode? MusicContentNode { get; set; }
}