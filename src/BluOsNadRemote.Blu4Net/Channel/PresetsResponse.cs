using System.Collections.Generic;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class PresetsResponse
{
    public Preset[] Presets = new Preset[0];

    internal static PresetsResponse Read(XmlReader reader)
    {
        reader.ReadRoot("presets");
        var presets = new List<Preset>();

        var handled = false;
        while (true)
        {
            if (!handled)
            {
                if (!reader.Read())
                {
                    break;
                }
            }
            handled = false;

            if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "presets")
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.LocalName == "preset")
            {
                // This reads only attributes; the reader remains on the start element, so we must
                // allow the outer loop to advance to the next node instead of suppressing Read().
                presets.Add(Preset.Read(reader));
            }
            else
            {
                reader.Skip();
                handled = true;
            }
        }

        return new PresetsResponse
        {
            Presets = presets.ToArray(),
        };
    }

    public override string ToString()
    {
        return Presets?.Length.ToString() ?? base.ToString();
    }

    public sealed class Preset
    {
        public string Name;

        public string Image;

        public string Url;

        public int Volume = -1;

        public int ID;

        internal static Preset Read(XmlReader reader)
        {
            var volume = reader.Attr("volume");
            var volumeValue = -1;
            if (volume != null && int.TryParse(volume, out var parsedVolume))
            {
                volumeValue = parsedVolume;
            }

            return new Preset
            {
                Name = reader.Attr("name"),
                Image = reader.Attr("image"),
                Url = reader.Attr("url"),
                Volume = volumeValue,
                ID = reader.AttrInt("id"),
            };
        }

        public override string ToString()
        {
            return ID.ToString();
        }
    }
}
