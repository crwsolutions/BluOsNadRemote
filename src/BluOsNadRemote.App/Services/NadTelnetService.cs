using BluOsNadRemote.App.Models;
using Nad4Net;

namespace BluOsNadRemote.App.Services;

public partial class NadTelnetService
{
    [Dependency]
    private readonly PreferencesRepository _preferencesRepository;

    internal NadRemote NadRemote { get; private set; }

    internal NadTelnetConnectResult Connect()
    {
        Disconnect();

        if (_preferencesRepository.SelectedEndpoint == null)
        {
            return new NadTelnetConnectResult("There is no endpoint. Go to settings");
        }

        NadRemote = new(_preferencesRepository.SelectedEndpoint.Uri);

        return NadTelnetConnectResult.Connected;
    }

    internal void Disconnect()
    {
        NadRemote?.Dispose();
        NadRemote = null;
    }
}
