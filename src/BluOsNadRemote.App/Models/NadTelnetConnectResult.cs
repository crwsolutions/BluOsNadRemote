namespace BluOsNadRemote.App.Models;

internal sealed record NadTelnetConnectResult(string? Message)
{
    internal bool IsConnected => Message == null;

    internal static NadTelnetConnectResult Connected => new(Message: null);
}
