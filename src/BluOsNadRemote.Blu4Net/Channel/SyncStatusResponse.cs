using System;
using System.Collections.Generic;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class SyncStatusResponse : ILongPollingResponse
{
    public string ETag { get; set; }

    public string ModelName;

    public string Name;

    public string Brand;

    public int Volume;

    public double Decibel;

    public string MAC;

    // Properties for when the player is in sync group
    public Slave[] Slave = new Slave[0];

    public Master Master;

    // Properties for when the player is part of a zone
    public bool IsZoneController;

    public string ZoneName;

    public string ZoneUngroupUrl;

    public ChannelMode? ChannelMode
    {
        get
        {
            if (string.IsNullOrEmpty(ChannelName))
            {
                return null;
            }

            if (Enum.TryParse<ChannelMode>(ChannelName, true, out var val))
            {
                return val;
            }

            return null;
        }
    }
    public string ChannelName;

    public ZoneSlave ZoneSlave;

    internal static SyncStatusResponse Read(XmlReader reader)
    {
        reader.ReadRoot("SyncStatus");
        var response = new SyncStatusResponse
        {
            ETag = reader.Attr("etag"),
            ModelName = reader.Attr("modelName"),
            Name = reader.Attr("name"),
            Brand = reader.Attr("brand"),
            Volume = reader.AttrInt("volume"),
            Decibel = reader.AttrDouble("db"),
            MAC = reader.Attr("mac"),
            IsZoneController = reader.AttrBool("zoneController"),
            ZoneName = reader.Attr("zone"),
            ZoneUngroupUrl = reader.Attr("zoneUngroup"),
            ChannelName = reader.Attr("channelName"),
        };

        var slaves = new List<Slave>();

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    break;
                }
                continue;
            }

            if (reader.LocalName == "slave")
            {
                slaves.Add(BluOsNadRemote.Blu4Net.Channel.Slave.Read(reader));
            }
            else if (reader.LocalName == "master")
            {
                response.Master = Master.Read(reader);
            }
            else if (reader.LocalName == "zoneSlave")
            {
                response.ZoneSlave = ZoneSlave.Read(reader);
            }
            else
            {
                reader.Skip();
            }
        }

        response.Slave = slaves.ToArray();
        return response;
    }

    public override string ToString()
    {
        return Name;
    }
}

public sealed class Master
{
    public int Port;

    public string Address;

    internal static Master Read(XmlReader reader)
    {
        var master = new Master
        {
            Port = reader.AttrInt("port"),
        };

        if (reader.IsEmptyElement)
        {
            return master;
        }

        master.Address = reader.ReadText();
        return master;
    }

    public override string ToString() => $"{Address}:{Port}";
}

public sealed class Slave
{
    public int Port;

    public string Address;

    internal static Slave Read(XmlReader reader)
    {
        return new Slave
        {
            Port = reader.AttrInt("port"),
            Address = reader.Attr("id"),
        };
    }

    public override string ToString() => $"{Address}:{Port}";
}

public sealed class ZoneSlave
{
    public string Address;

    public int Port;

    public bool IsZoneSlave;

    public ChannelMode? ChannelMode
    {
        get
        {
            if (string.IsNullOrEmpty(ChannelName))
            {
                return null;
            }

            if (Enum.TryParse<ChannelMode>(ChannelName, true, out var val))
            {
                return val;
            }

            return null;
        }
    }
    public string ChannelName;

    public string Name;

    public string Model;

    public string ModelName;

    internal static ZoneSlave Read(XmlReader reader)
    {
        return new ZoneSlave
        {
            Address = reader.Attr("id"),
            Port = reader.AttrInt("port"),
            IsZoneSlave = reader.AttrBool("zoneSlave"),
            ChannelName = reader.Attr("channelName"),
            Name = reader.Attr("name"),
            Model = reader.Attr("model"),
            ModelName = reader.Attr("modelName"),
        };
    }

    public override string ToString() => $"{Name} ({Address}:{Port})";
}
