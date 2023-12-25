using BluOsNadRemote.App.Models;
using Nad4Net;

namespace BluOsNadRemote.App.Services;

public partial class NadTelnetService
{
    [Dependency]
    private readonly ConfigurationService _configurationService;

    internal NadRemote NadRemote { get; private set; }

    internal NadTelnetConnectResult Connect()
    {
        Disconnect();

        if (_configurationService.SelectedEndpoint == null)
        {
            return new NadTelnetConnectResult("There is no endpoint. Go to settings");
        }

        NadRemote = new(_configurationService.SelectedEndpoint.Uri);

        return NadTelnetConnectResult.Connected;
    }

    internal void Disconnect()
    {
        NadRemote?.Dispose();
        NadRemote = null;
    }
}
