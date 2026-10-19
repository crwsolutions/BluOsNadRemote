using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BluOsNadRemote.Blu4Net;

/// <summary>
/// Discovers BluOS players on the local network with the Lenbrook Service Discovery
/// Protocol (LSDP, see docs/BluOS-Custom-Integration-API_v1.7.md section 13.1): a UDP
/// broadcast query on port 11430, answered by Announce messages.
/// </summary>
public static class LsdpDiscoverer
{
    private const int LsdpPort = 11430;
    private const int HeaderLength = 6;
    private const byte ProtocolVersion = 1;
    private const int QueryMessageType = 0x51;
    private const int AnnounceMessageType = 0x41;
    private const ushort ClassBluOsPlayer = 0x0001;
    private static readonly byte[] Magic = [0x4C, 0x53, 0x44, 0x50]; // "LSDP"

    private static readonly byte[] QueryPacket = BuildQueryPacket();

    /// <summary>
    /// A node discovered through LSDP.
    /// </summary>
    /// <param name="NodeId">Unique node ID (usually a MAC address).</param>
    /// <param name="Address">IPv4 address of the node.</param>
    public sealed record LsdpHost(string NodeId, IPAddress Address);

    /// <summary>
    /// Broadcasts an LSDP query for the BluOS Player class and listens for Announce
    /// messages for the given window. The query is sent to the directed broadcast
    /// address of every active IPv4 subnet, plus the limited broadcast address
    /// (255.255.255.255) which the OS delivers over the default interface. This
    /// keeps discovery working on platforms (e.g. Android Wi-Fi) that report a
    /// prefix length of zero or refuse to bind to a specific interface address.
    /// Discovery failures (no network, broadcast refused, timeout) yield an empty
    /// result instead of an exception.
    /// </summary>
    public static async Task<IReadOnlyList<LsdpHost>> DiscoverAsync(TimeSpan? listenWindow = null, CancellationToken cancellationToken = default)
    {
        var window = listenWindow ?? TimeSpan.FromSeconds(5);
        var found = new ConcurrentDictionary<IPAddress, LsdpHost>();

        try
        {
            var destinations = BroadcastDestinations();
            using var client = new UdpClient(AddressFamily.InterNetwork);
            client.EnableBroadcast = true;

            // LSDP uses all packets to *and from* port 11430. The socket must be bound
            // to 11430 so that the broadcast/unicast Announce responses (sent *to*
            // 11430) are delivered to us. A wildcard (0.0.0.0) bind is safe on all
            // platforms; only a per-interface bind is the problem.
            client.Client.Bind(new IPEndPoint(IPAddress.Any, LsdpPort));
            Debug.WriteLine($"[LsdpDiscoverer] socket bound to {client.Client.LocalEndPoint}");

            foreach (var destination in destinations)
            {
                try
                {
                    await client.SendAsync(QueryPacket, destination).ConfigureAwait(false);
                    Debug.WriteLine($"[LsdpDiscoverer] query sent to {destination}");
                }
                catch (Exception ex)
                {
                    var code = ex is SocketException socketException ? (int)socketException.SocketErrorCode : -1;
                    Debug.WriteLine($"[LsdpDiscoverer] query to {destination} failed: {ex.GetType().Name} (SocketError={code}) {ex.Message}");
                }
            }

            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using (linked)
            {
                linked.CancelAfter((int)window.TotalMilliseconds);

                try
                {
                    while (!linked.Token.IsCancellationRequested)
                    {
                        var datagram = await client.ReceiveAsync(linked.Token).ConfigureAwait(false);
                        var hosts = Parse(datagram.Buffer);
                        Debug.WriteLine($"[LsdpDiscoverer] datagram {datagram.Buffer.Length} bytes from {datagram.RemoteEndPoint} -> {hosts.Count} player node(s)");
                        foreach (var host in hosts)
                        {
                            if (found.TryAdd(host.Address, host))
                            {
                                Debug.WriteLine($"[LsdpDiscoverer] node {host.NodeId} at {host.Address}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Listen window elapsed.
                }
                catch (Exception ex)
                {
                    var code = ex is SocketException socketException ? (int)socketException.SocketErrorCode : -1;
                    Debug.WriteLine($"[LsdpDiscoverer] receive stopped: {ex.GetType().Name} (SocketError={code}) {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // Discovery is best effort (no usable socket, no network); report whatever was found so far.
            Debug.WriteLine($"[LsdpDiscoverer] discovery failed: {ex.GetType().Name} ({ex.Message})");
        }

        Debug.WriteLine($"[LsdpDiscoverer] scan complete: {found.Count} node(s) found");
        return found.Values.ToList();
    }

    /// <summary>
    /// Returns the exact bytes of the LSDP query packet for the BluOS Player class:
    /// header (06 "LSDP" 01) + query message (05 51 01 00 01).
    /// </summary>
    internal static byte[] BuildQueryPacket() =>
        [
            HeaderLength, Magic[0], Magic[1], Magic[2], Magic[3], ProtocolVersion,
            0x05, QueryMessageType, 0x01, 0x00, 0x01
        ];

    /// <summary>
    /// Parses one LSDP datagram and returns the BluOS Player hosts it announces.
    /// Malformed datagrams and datagrams without player announcements yield an empty
    /// list. Message blocks are walked by length so unknown future types are skipped;
    /// Delete and unicast query messages are ignored. A node may be split over several
    /// announce blocks (doc Note 2); all of them are returned, deduplication happens
    /// on the caller side.
    /// </summary>
    public static IReadOnlyList<LsdpHost> Parse(ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length < HeaderLength || datagram[0] != HeaderLength || !datagram.Slice(1, Magic.Length).SequenceEqual(Magic) || datagram[5] != ProtocolVersion)
        {
            return [];
        }

        var hosts = new List<LsdpHost>();
        var offset = HeaderLength;

        while (offset < datagram.Length)
        {
            var length = datagram[offset];
            if (length < 2 || offset + length > datagram.Length)
            {
                break; // Truncated or malformed block; stop parsing this datagram.
            }

            var message = datagram[(offset + 2)..(offset + length)];

            switch (datagram[offset + 1])
            {
                case AnnounceMessageType:
                    var host = ParseAnnounceBlock(message);
                    if (host != null)
                    {
                        hosts.Add(host);
                    }

                    break;

                default:
                    // Delete (0x44), unicast query (0x52) and any future message types are skipped.
                    break;
            }

            offset += length;
        }

        return hosts;
    }

    private static List<IPEndPoint> BroadcastDestinations()
    {
        var destinations = new List<IPEndPoint>();
        var subnets = new List<string>();

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            var unicastAddresses = networkInterface.GetIPProperties().UnicastAddresses;
            if (unicastAddresses.Count == 0)
            {
                continue;
            }

            var unicast = unicastAddresses.FirstOrDefault(address => address.Address.AddressFamily == AddressFamily.InterNetwork);
            if (unicast is null)
            {
                continue; // No IPv4 address on this interface.
            }

            // Prefer the explicit IPv4 mask (more reliably populated than PrefixLength,
            // which is often 0 on Android Wi-Fi).
            uint mask = 0;
            if (unicast.IPv4Mask is not null)
            {
                mask = UnicastToInteger(unicast.IPv4Mask);
            }
            else if (unicast.PrefixLength > 0 && unicast.PrefixLength < 32)
            {
                mask = 0xFFFFFFFFu << (32 - unicast.PrefixLength);
            }

            Debug.WriteLine($"[LsdpDiscoverer] interface {networkInterface.Name}: {unicast.Address} (mask 0x{mask:X8})");
            if (mask == 0 || mask == 0xFFFFFFFFu)
            {
                continue; // No usable subnet mask; the limited broadcast still covers this interface.
            }

            var unicastBytes = unicast.Address.GetAddressBytes();
            var maskBytes = GetMaskBytes(mask);
            // Subnet broadcast: for each octet, keep the unicast value where the mask
            // bit is set, use 255 where it is not. No carry propagation — the host
            // portion simply becomes all-ones.
            var broadcastBytes = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                broadcastBytes[i] = (byte)((unicastBytes[i] & maskBytes[i]) | (byte)~maskBytes[i]);
            }

            var subnetKey = $"{unicastBytes[0]}.{unicastBytes[1]}.{unicastBytes[2]}";
            if (subnets.Contains(subnetKey))
            {
                continue;
            }

            subnets.Add(subnetKey);
            destinations.Add(new IPEndPoint(new IPAddress(broadcastBytes), LsdpPort));
        }

        // The limited broadcast is delivered by the OS over the default interface; it
        // is the fallback when no subnet mask is known (common on Android Wi-Fi).
        destinations.Add(new IPEndPoint(IPAddress.Broadcast, LsdpPort));
        return destinations;
    }

    private static LsdpHost ParseAnnounceBlock(ReadOnlySpan<byte> block)
    {
        // The Length and Type bytes are already stripped; the rest is the announce
        // header (NodeID, Address, Count) followed by the announce records.
        if (block.Length < 3)
        {
            return null;
        }

        var cursor = 0;

        var nodeIdentifierLength = block[cursor++];
        if (cursor + nodeIdentifierLength + 1 > block.Length)
        {
            return null;
        }

        var nodeId = FormatNodeId(block.Slice(cursor, nodeIdentifierLength));
        cursor += nodeIdentifierLength;
        var addressLength = block[cursor++];
        if (addressLength != 4 || cursor + addressLength > block.Length)
        {
            return null; // Only IPv4 addresses are supported.
        }

        var address = new IPAddress(block.Slice(cursor, addressLength));
        cursor += addressLength;

        if (cursor >= block.Length)
        {
            return null;
        }

        var count = block[cursor++];
        for (var i = 0; i < count; i++)
        {
            // Class (2, big endian).
            if (cursor + 2 > block.Length)
            {
                return null; // Malformed record.
            }

            var recordClass = (ushort)((block[cursor] << 8) | block[cursor + 1]);
            cursor += 2;

            if (recordClass == ClassBluOsPlayer)
            {
                return new LsdpHost(nodeId, address);
            }

            // TXT records follow (variable length) and are not used; skip them so the
            // next record can be read.
            if (cursor >= block.Length)
            {
                return null;
            }

            var txtCount = block[cursor++];
            for (var t = 0; t < txtCount; t++)
            {
                if (cursor + 2 > block.Length)
                {
                    return null; // Malformed TXT record.
                }

                var keyLength = block[cursor++];
                var valueLength = block[cursor++];
                if (cursor + keyLength + valueLength > block.Length)
                {
                    return null; // Malformed TXT record.
                }

                cursor += keyLength + valueLength;
            }
        }

        return null;
    }

    private static uint UnicastToInteger(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    private static byte[] GetMaskBytes(uint mask) =>
        [
            (byte)(mask >> 24),
            (byte)(mask >> 16),
            (byte)(mask >> 8),
            (byte)mask
        ];

    /// <summary>
    /// Formats a raw Node ID. Firmware may send it as a printable ASCII string
    /// (e.g. "90:56:82:B0:3E:12") or as raw bytes (e.g. a 6-byte MAC). Printable
    /// strings are returned as-is; binary data is rendered as colon-separated hex.
    /// </summary>
    private static string FormatNodeId(ReadOnlySpan<byte> nodeId)
    {
        var isPrintable = true;
        for (var i = 0; i < nodeId.Length; i++)
        {
            if (nodeId[i] < 0x20 || nodeId[i] > 0x7E)
            {
                isPrintable = false;
                break;
            }
        }

        if (isPrintable)
        {
            return Encoding.ASCII.GetString(nodeId);
        }

        var parts = new string[nodeId.Length];
        for (var i = 0; i < nodeId.Length; i++)
        {
            parts[i] = nodeId[i].ToString("X2");
        }

        return string.Join(":", parts);
    }
}
