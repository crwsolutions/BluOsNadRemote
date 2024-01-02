using Blu4Net;
using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Utils;
using System.Reactive.Linq;

namespace BluOsNadRemote.App.Services;

public sealed partial class BluPlayerService
{
    [Dependency]
    private readonly ConfigurationService _configurationService;

    public bool IsConnected { get; set; }

    public async Task<BluPlayerConnectResult> ConnectAsync()
    {
        if (_configurationService.SelectedEndpoint == null)
        {
            return new BluPlayerConnectResult("No connection", false);
        }

        try
        {
            var uri = _configurationService.SelectedEndpoint.Uri;
            BluPlayer = await BluPlayer.Connect(uri);
            Console.WriteLine($"Player: {BluPlayer}");
        }
        catch (Exception exception)
        {
            return new BluPlayerConnectResult($"Could not connect: {exception.Message}", false);
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
        var uri = await BluEnvironment.ResolveEndpoints().FirstOrDefaultAsync();

        if (uri == null)
        {
            return new BluPlayerDiscoverResult("Player not found", false);
        }

        _configurationService.SetEndpoint(uri);

        var connectResult = await ConnectAsync();

        return new BluPlayerDiscoverResult(connectResult.Message, true);
    }

    public BluPlayer BluPlayer { get; private set; }

    public MusicContentEntry MusicContentEntry { get; set; }
    public MusicContentNode MusicContentNode { get; set; }
}