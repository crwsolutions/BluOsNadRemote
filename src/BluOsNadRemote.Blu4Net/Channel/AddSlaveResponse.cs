using System.Collections.Generic;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class AddSlaveResponse
{
    public Slave[] Slave = new Slave[0];

    internal static AddSlaveResponse Read(XmlReader reader)
    {
        reader.ReadRoot("addSlave");
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
            else
            {
                reader.Skip();
            }
        }

        return new AddSlaveResponse
        {
            Slave = slaves.ToArray(),
        };
    }
}
