using System;

namespace Nad4Net;

/// <summary>
/// Why connecting to the NAD telnet endpoint failed.
/// </summary>
public enum NadConnectReason
{
    /// <summary>The connect/init-read took longer than the allowed timeout.</summary>
    Timeout,

    /// <summary>The host could not be reached (socket refused/reset/host down).</summary>
    Unreachable,

    /// <summary>The TCP connection was established but the telnet negotiation failed.</summary>
    Negotiation,

    /// <summary>No endpoint is configured in the app.</summary>
    NoEndpoint,
}

/// <summary>
/// Thrown when a telnet connection to a NAD endpoint could not be established.
/// Carries a friendly <see cref="Reason"/> so the UI can show a localized message
/// instead of a raw socket exception.
/// </summary>
public class NadConnectException : Exception
{
    public NadConnectException(string? host, NadConnectReason reason)
        : base(host is null ? "Could not connect" : $"Could not connect to {host}")
    {
        Host = host;
        Reason = reason;
    }

    public NadConnectException(string? host, NadConnectReason reason, Exception innerException)
        : base(host is null ? "Could not connect" : $"Could not connect to {host}", innerException)
    {
        Host = host;
        Reason = reason;
    }

    public string? Host { get; }

    public NadConnectReason Reason { get; }
}
