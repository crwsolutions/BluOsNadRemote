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

    internal async Task<NadTelnetConnectResult> ConnectAsync()
    {
        Disconnect();

        if (_endpointRepository.SelectedEndpoint == null)
        {
            return NadTelnetConnectResult.NoEndpoint;
        }

        var remote = new NadRemote(_endpointRepository.SelectedEndpoint.Uri);
        NadRemote = remote;

        try
        {
            await remote.ConnectAsync();
            return NadTelnetConnectResult.Connected;
        }
        catch (NadConnectException exception)
        {
            Disconnect();
            return NadTelnetConnectResult.Failed(exception.Reason, exception.Host);
        }
        catch (OperationCanceledException)
        {
            Disconnect();
            return NadTelnetConnectResult.Failed(NadConnectReason.Timeout, remote.Endpoint.Host);
        }
    }

    internal void Disconnect()
    {
        NadRemote?.Dispose();
        NadRemote = null;
    }
}
