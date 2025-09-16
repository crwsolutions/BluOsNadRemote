using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;


[XmlInclude(typeof(PlaylistLoadedResponse))]
[XmlInclude(typeof(StateResponse))]
public class LoadedResponse
{
}


[XmlRoot("loaded")]
public sealed class PlaylistLoadedResponse : LoadedResponse
{
    [XmlAttribute("service")]
    public string Service;

    [XmlElement("entries")]
    public int Entries;

    public override string ToString()
    {
        return $"{Service} {Entries}";
    }
}
