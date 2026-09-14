using System.Net;
using System.Text;
using Xunit;

namespace BluOsNadRemote.Blu4Net.Tests;

public class LsdpDiscovererTests
{
    private static readonly byte[] Header = { 0x06, 0x4C, 0x53, 0x44, 0x50, 0x01 };

    private readonly record struct Record(ushort Class, byte[] Txt);

    [Fact]
    public void BuildQueryPacket_MatchesSpecifiedBytes()
    {
        // Header (06 "LSDP" 01) + query message (05 Q 01 00 01).
        var expected = new byte[] { 0x06, 0x4C, 0x53, 0x44, 0x50, 0x01, 0x05, 0x51, 0x01, 0x00, 0x01 };
        Assert.Equal(expected, LsdpDiscoverer.BuildQueryPacket());
    }

    [Fact]
    public void Parse_PlayerAnnounce_ReturnsHostWithNodeIdAndAddress()
    {
        var datagram = Append(Header, Announce("AA:BB:CC:DD:EE:FF", "192.168.1.42",
            new[] { new Record(0x0005, Txt()), new Record(0x0001, Txt(new[] { ("bluos", "2.1.5-1.23"), ("product", "BNC-365") })) }));

        var hosts = LsdpDiscoverer.Parse(datagram);

        var host = Assert.Single(hosts);
        Assert.Equal("AA:BB:CC:DD:EE:FF", host.NodeId);
        Assert.Equal(IPAddress.Parse("192.168.1.42"), host.Address);
    }

    [Fact]
    public void Parse_NonPlayerClass_ReturnsNothing()
    {
        var datagram = Append(Header, Announce("AA:BB:CC:DD:EE:FF", "192.168.1.42",
            new[] { new Record(0x0002, Txt()), new Record(0x0005, Txt()) }));
        Assert.Empty(LsdpDiscoverer.Parse(datagram));
    }

    [Fact]
    public void Parse_ClassIsReadBigEndian()
    {
        // Class bytes 01 00 read as little endian would be the player class 0x0001;
        // big endian it is 0x0100 and must be ignored.
        var datagram = Append(Header, Announce("AA:BB:CC:DD:EE:FF", "192.168.1.42", new[] { new Record(0x0100, Txt()) }));
        Assert.Empty(LsdpDiscoverer.Parse(datagram));
    }

    [Fact]
    public void Parse_UnknownMessageType_IsSkippedByLength()
    {
        var unknownBlock = new byte[] { 0x08, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var datagram = Append(Header, unknownBlock, Announce("AA:BB:CC:DD:EE:FF", "10.0.0.7", new[] { new Record(0x0001, Txt()) }));

        var hosts = LsdpDiscoverer.Parse(datagram);

        var host = Assert.Single(hosts);
        Assert.Equal(IPAddress.Parse("10.0.0.7"), host.Address);
    }

    [Fact]
    public void Parse_SplitAnnounce_ReturnsHostPerBlock()
    {
        // Doc Note 2: one node may be split over several announce blocks, each with
        // the full header and node info.
        var datagram = Append(Header,
            Announce("AA:BB:CC:DD:EE:FF", "192.168.1.42", new[] { new Record(0x0001, Txt(new[] { ("first", "part") })) }),
            Announce("AA:BB:CC:DD:EE:FF", "192.168.1.42", new[] { new Record(0x0001, Txt(new[] { ("second", "part") })) }));

        var hosts = LsdpDiscoverer.Parse(datagram);

        Assert.Equal(2, hosts.Count);
        Assert.All(hosts, host => Assert.Equal(IPAddress.Parse("192.168.1.42"), host.Address));
    }

    [Fact]
    public void Parse_DeleteAndUnicastQuery_AreIgnored()
    {
        var unicastQuery = new byte[] { 0x05, 0x52, 0x01, 0x00, 0x01 };
        var datagram = Append(Header, Delete("AA:BB:CC:DD:EE:FF", new[] { (ushort)0x0001 }), unicastQuery);
        Assert.Empty(LsdpDiscoverer.Parse(datagram));
    }

    [Theory]
    [InlineData(-1)] // Too short.
    [InlineData(0)]  // Wrong header length.
    [InlineData(1)]  // Wrong magic.
    [InlineData(5)]  // Wrong version.
    public void Parse_InvalidHeader_ReturnsNothing(int corruptedField)
    {
        var datagram = (byte[])Header.Clone();

        switch (corruptedField)
        {
            case -1:
                Array.Resize(ref datagram, 5);
                break;
            case 0:
                datagram[0] = 0x07;
                break;
            case 1:
                datagram[2] = 0x58; // "LXDP"
                break;
            case 5:
                datagram[5] = 0x02;
                break;
        }

        Assert.Empty(LsdpDiscoverer.Parse(datagram));
    }

    [Fact]
    public void Parse_TruncatedMessageBlock_StopsWithoutThrowing()
    {
        var truncatedBlock = new byte[] { 0x20, 0x41, 0x06, 0xAA }; // Length claims 32 bytes, only 3 follow.
        var datagram = Append(Header, truncatedBlock);
        Assert.Empty(LsdpDiscoverer.Parse(datagram));
    }

    [Fact]
    public void Parse_EmptyAnnounceBlock_ReturnsNothing()
    {
        // An announce block carrying only Length + Type (no node info) must be tolerated.
        var emptyBlock = new byte[] { 0x02, 0x41 };
        var datagram = Append(Header, emptyBlock);
        Assert.Empty(LsdpDiscoverer.Parse(datagram));
    }

    private static byte[] Append(byte[] header, params byte[][] blocks)
    {
        var datagram = new List<byte>(header);
        foreach (var block in blocks)
        {
            datagram.AddRange(block);
        }

        return [.. datagram];
    }

    private static byte[] Announce(string nodeId, string address, Record[] records)
    {
        var body = new List<byte>
        {
            (byte)nodeId.Length
        };
        body.AddRange(Encoding.ASCII.GetBytes(nodeId));
        body.Add(4);
        body.AddRange(IPAddress.Parse(address).GetAddressBytes());
        body.Add((byte)records.Length);
        foreach (var record in records)
        {
            body.Add((byte)(record.Class >> 8)); // Class is big endian.
            body.Add((byte)record.Class);
            body.AddRange(record.Txt);
        }

        var block = new List<byte> { (byte)(body.Count + 2), 0x41 };
        block.AddRange(body);
        return [.. block];
    }

    private static byte[] Delete(string nodeId, ushort[] classes)
    {
        var body = new List<byte>
        {
            (byte)nodeId.Length
        };
        body.AddRange(Encoding.ASCII.GetBytes(nodeId));
        body.Add((byte)classes.Length);
        foreach (var cls in classes)
        {
            body.Add((byte)(cls >> 8));
            body.Add((byte)cls);
        }

        var block = new List<byte> { (byte)(body.Count + 2), 0x44 };
        block.AddRange(body);
        return [.. block];
    }

    private static byte[] Txt(params (string Key, string Value)[] pairs)
    {
        var body = new List<byte> { (byte)pairs.Length };
        foreach (var (key, value) in pairs)
        {
            var keyBytes = Encoding.ASCII.GetBytes(key);
            var valueBytes = Encoding.ASCII.GetBytes(value);
            body.Add((byte)keyBytes.Length);
            body.AddRange(keyBytes);
            body.Add((byte)valueBytes.Length);
            body.AddRange(valueBytes);
        }

        return [.. body];
    }
}
