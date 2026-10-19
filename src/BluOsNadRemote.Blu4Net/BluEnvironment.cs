using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BluOsNadRemote.Blu4Net;

public sealed class BluEnvironment
{
    public static int DefaultEndpointPort = 11000;
    public static TimeSpan DefaultScanTimeout = TimeSpan.FromSeconds(5);
    private const int MaxScanAttempts = 3;

    /// <summary>
    /// A discovered BluOS player endpoint, including the name reported by the player.
    /// </summary>
    public sealed record Endpoint(Uri Uri, string Name);

    /// <summary>
    /// Scans the local network for BluOS players using LSDP (Lenbrook Service
    /// Discovery Protocol). While a scan finds nothing it is repeated up to
    /// <see cref="MaxScanAttempts"/> times. Each discovered host is verified with a
    /// connection attempt, so the returned endpoints are reachable and carry the
    /// player name.
    /// </summary>
    /// <param name="acceptLanguage">The preferred language of the player responses.</param>
    public static async Task<IReadOnlyList<Endpoint>> ResolveEndpointsAsync(CultureInfo acceptLanguage = null)
    {
        var hosts = new List<LsdpDiscoverer.LsdpHost>();

        for (var attempt = 0; attempt < MaxScanAttempts && hosts.Count == 0; attempt++)
        {
            hosts = [.. await LsdpDiscoverer.DiscoverAsync(DefaultScanTimeout).ConfigureAwait(false)];
        }

        var endpoints = new List<Endpoint>();

        foreach (var host in hosts)
        {
            try
            {
                var player = await BluPlayer.Connect(GetEndpoint(host), acceptLanguage).ConfigureAwait(false);
                endpoints.Add(new Endpoint(player.Endpoint, player.Name));
            }
            catch (Exception)
            {
                // A discovered host that does not answer is skipped.
            }
        }

        return endpoints;
    }

    private static Uri GetEndpoint(LsdpDiscoverer.LsdpHost host)
    {
        return new UriBuilder("http", host.Address.ToString(), DefaultEndpointPort).Uri;
    }
}
