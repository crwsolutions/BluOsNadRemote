using Blu4Net;

namespace BluOsNadRemote.App.Utils;
internal static class BluOsHost
{
    internal static string ToUriString(string host)
    {
        // Check if the host is an IPv6 address
        if (System.Net.IPAddress.TryParse(host, out var ipAddress) && ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // For IPv6 addresses, wrap in square brackets
            return $"http://[{host}]:{BluEnvironment.DefaultEndpointPort}/";
        }
        // For IPv4 addresses or hostnames, use normal format
        return $"http://{host}:{BluEnvironment.DefaultEndpointPort}/";
    }
}
