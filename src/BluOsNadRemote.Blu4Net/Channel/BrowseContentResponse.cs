using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("browse")]
public sealed class BrowseContentResponse
{
    [XmlAttribute("serviceName")]
    public string ServiceName;

    [XmlAttribute("serviceIcon")]
    public string ServiceIcon;

    [XmlAttribute("searchKey")]
    public string SearchKey;

    [XmlAttribute("nextKey")]
    public string NextKey;

    [XmlElement("item")]
    public Item[] Items = new Item[0];

    [XmlRoot("item")]
    public sealed class Item
    {
        [XmlAttribute("browseKey")]
        public string BrowseKey;

        [XmlAttribute("type")]
        public string Type;

        [XmlAttribute("text")]
        public string Text;

        [XmlAttribute("text2")]
        public string Text2;

        [XmlAttribute("contextMenuKey")]
        public string ContextMenuKey;

        [XmlAttribute("playURL")]
        public string PlayURL;

        [XmlAttribute("autoplayURL")]
        public string AutoplayURL;

        [XmlAttribute("actionURL")]
        public string ActionURL;

        [XmlAttribute("image")]
        public string Image;

        public override string ToString()
        {
            return Text;
        }
    }

    [XmlElement("category")]
    public Category[] Categories = [];

    [XmlRoot("category")]
    public sealed class Category
    {
        [XmlAttribute("text")]
        public string Text;

        [XmlElement("item")]
        public Item[] Items = new Item[0];

        [XmlAttribute("nextKey")]
        public string NextKey;

        public override string ToString()
        {
            return Text;
        }
    }
}
