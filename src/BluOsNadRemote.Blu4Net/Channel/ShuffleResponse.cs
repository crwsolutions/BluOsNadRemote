using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel
{
    [XmlRoot("playlist")]
    public class ShuffleResponse
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlAttribute("length")]
        public int Length;

        [XmlAttribute("modified")]
        public int Modified;

        [XmlAttribute("shuffle")]
        public int Shuffle;

        public override string ToString()
        {
            return Shuffle.ToString();
        }
    }
}
