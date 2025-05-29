using BluOsNadRemote.App.Models;
using BluOsNadRemote.App.Repositories;
using Nad4Net;
using System.Diagnostics.CodeAnalysis;

namespace BluOsNadRemote.App.Services;

public partial class NadTelnetService
{
    [Dependency]
    private readonly EndpointRepository _endpointRepository;

    internal NadRemote? NadRemote { get; private set; }

    [MemberNotNullWhen(true, nameof(NadRemote))]
    internal bool IsConnected => NadRemote?.IsConnected is true;

    internal NadTelnetConnectResult Connect()
    {
        Disconnect();

        if (_endpointRepository.SelectedEndpoint == null)
        {
            return new NadTelnetConnectResult("There is no endpoint. Go to settings");
        }

        NadRemote = new(_endpointRepository.SelectedEndpoint.Uri);

        return NadTelnetConnectResult.Connected;
    }

    internal void Disconnect()
    {
        NadRemote?.Dispose();
        NadRemote = null;
    }
}
