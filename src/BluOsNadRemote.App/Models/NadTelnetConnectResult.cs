using Nad4Net;

namespace BluOsNadRemote.App.Models;

/// <summary>
/// Result of a telnet connect attempt. <see cref="IsConnected"/> is true only on success;
/// otherwise <see cref="Reason"/> and <see cref="Host"/> describe the friendly failure.
/// </summary>
internal sealed record NadTelnetConnectResult(bool IsConnected, NadConnectReason Reason, string? Host)
{
    internal static NadTelnetConnectResult Connected { get; } = new(IsConnected: true, Reason: NadConnectReason.NoEndpoint, Host: null);

    internal static NadTelnetConnectResult NoEndpoint { get; } = new(IsConnected: false, Reason: NadConnectReason.NoEndpoint, Host: null);

    internal static NadTelnetConnectResult Failed(NadConnectReason reason, string? host) => new(IsConnected: false, Reason: reason, Host: host);
}
