using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("deleted")]
public class DeleteResponse
{
    [XmlText]
    public int ID;

    public override string ToString()
    {
        return ID.ToString();
    }
}
