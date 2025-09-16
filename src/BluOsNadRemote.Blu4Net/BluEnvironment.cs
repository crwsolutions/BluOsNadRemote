using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using Zeroconf;

namespace BluOsNadRemote.Blu4Net;

public sealed class BluEnvironment
{
    public static int DefaultEndpointPort = 11000;
    public static TimeSpan DefaultScanTimeout = TimeSpan.FromSeconds(5);

    private static Uri GetEndpoint(IZeroconfHost host)
    {
        var address = IPAddress.Parse(host.IPAddress);
        var port = DefaultEndpointPort;

        if (host.Services.TryGetValue("_musc._tcp.local.", out var service))
        {
            port = service.Port;
        }

        return new UriBuilder("http", address.ToString(), port).Uri;
    }

    public static IObservable<Uri> ResolveEndpoints(TimeSpan scanTime)
    {
        return ZeroconfResolver
          .Resolve("_musc._tcp.local.", scanTime)
          .Where(element => element.IPAddress != null)
          .Select(host => GetEndpoint(host));
    }

    public static IObservable<Uri> ResolveEndpoints()
    {
        return ResolveEndpoints(DefaultScanTimeout);
    }
}
