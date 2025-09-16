using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("addSlave")]
public sealed class AddSlaveResponse
{
    [XmlElement("slave")]
    public Slave[] Slave = new Slave[0];

}