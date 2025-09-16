using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("id")]
public sealed class IdResponse
{
    [XmlText()]
    public int ID;

    public override string ToString()
    {
        return ID.ToString();
    }
}
