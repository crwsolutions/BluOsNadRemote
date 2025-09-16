using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("saved")]
public sealed class SavedResponse
{
    [XmlElement("entries")]
    public int Entries;

    public override string ToString()
    {
        return Entries.ToString();
    }
}
