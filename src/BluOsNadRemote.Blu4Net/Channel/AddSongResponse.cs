using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("addsong")]
public sealed class AddSongResponse : LoadedResponse
{
    [XmlAttribute("id")]
    public int ID;

    [XmlAttribute("count")]
    public int Count;

    [XmlAttribute("length")]
    public int Length;

    public override string ToString()
    {
        return ID.ToString();
    }
}
