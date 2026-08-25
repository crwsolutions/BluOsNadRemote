using System.Collections.Generic;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class BrowseContentResponse
{
    public string ServiceName;

    public string ServiceIcon;

    public string SearchKey;

    public string NextKey;

    public Item[] Items = new Item[0];

    public Category[] Categories = [];

    internal static BrowseContentResponse Read(XmlReader reader)
    {
        if (reader.LocalName == "error")
        {
            // /Browse can return an <error> root (with <message>/<detail> children). The old
            // attribute-based deserializer returned null here (null-reference risk in the
            // callers); the parser intentionally returns an object with empty collections.
            reader.Skip();
            return new BrowseContentResponse();
        }

        reader.ReadRoot("browse");
        var response = new BrowseContentResponse
        {
            ServiceName = reader.Attr("serviceName"),
            ServiceIcon = reader.Attr("serviceIcon"),
            SearchKey = reader.Attr("searchKey"),
            NextKey = reader.Attr("nextKey"),
        };

        var items = new List<Item>();
        var categories = new List<Category>();

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

            if (reader.LocalName == "item")
            {
                items.Add(Item.Read(reader));
            }
            else if (reader.LocalName == "category")
            {
                categories.Add(Category.Read(reader));
            }
            else
            {
                reader.Skip();
            }
        }

        response.Items = items.ToArray();
        response.Categories = categories.ToArray();
        return response;
    }

    public sealed class Item
    {
        public string BrowseKey;

        public string Type;

        public string Text;

        public string Text2;

        public string ContextMenuKey;

        public string PlayURL;

        public string AutoplayURL;

        public string ActionURL;

        public string Image;

        internal static Item Read(XmlReader reader)
        {
            var item = new Item
            {
                BrowseKey = reader.Attr("browseKey"),
                Type = reader.Attr("type"),
                Text = reader.Attr("text"),
                Text2 = reader.Attr("text2"),
                ContextMenuKey = reader.Attr("contextMenuKey"),
                PlayURL = reader.Attr("playURL"),
                AutoplayURL = reader.Attr("autoplayURL"),
                ActionURL = reader.Attr("actionURL"),
                Image = reader.Attr("image"),
            };

            if (reader.IsEmptyElement)
            {
                return item;
            }

            // skip nested content, e.g. a <contextMenu> element (withContextMenuItems=1)
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    reader.Skip();
                }
            }

            return item;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public sealed class Category
    {
        public string Text;

        public Item[] Items = new Item[0];

        public string NextKey;

        internal static Category Read(XmlReader reader)
        {
            var category = new Category
            {
                Text = reader.Attr("text"),
                NextKey = reader.Attr("nextKey"),
            };

            if (reader.IsEmptyElement)
            {
                return category;
            }

            var items = new List<Item>();

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

                if (reader.LocalName == "item")
                {
                    items.Add(Item.Read(reader));
                }
                else
                {
                    reader.Skip();
                }
            }

            category.Items = items.ToArray();
            return category;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
