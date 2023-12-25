namespace BluOsNadRemote.App.Models;

internal sealed class NadTelnetConnectResult
{
    internal NadTelnetConnectResult(string message)
    {
        Message = message;
    }

    internal string Message { get; }

    internal bool IsConnected => Message == null;

    internal static NadTelnetConnectResult Connected = new(null);
}
