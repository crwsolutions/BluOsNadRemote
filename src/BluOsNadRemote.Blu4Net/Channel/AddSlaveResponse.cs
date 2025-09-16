using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("addSlave")]
public class AddSlaveResponse
{
    [XmlElement("slave")]
    public Slave[] Slave = new Slave[0];

}