using System;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public class LoadedResponse
{
    /// <summary>
    /// Dispatches on the root element name: <c>loaded</c> → <see cref="PlaylistLoadedResponse"/>.
    /// (A <c>state</c> root cannot be represented by this hierarchy and is therefore rejected,
    /// mirroring the previous deserializer behaviour which threw for that case.)
    /// Throws <see cref="InvalidOperationException"/> for any other root.
    /// </summary>
    internal static LoadedResponse Read(XmlReader reader)
    {
        return reader.LocalName switch
        {
            "loaded" => PlaylistLoadedResponse.Read(reader),
            _ => throw new InvalidOperationException($"Encountered invalid xml root element <{reader.LocalName}>")
        };
    }
}


public sealed class PlaylistLoadedResponse : LoadedResponse
{
    public string Service;

    public int Entries;

    new internal static PlaylistLoadedResponse Read(XmlReader reader)
    {
        reader.ReadRoot("loaded");
        var response = new PlaylistLoadedResponse
        {
            Service = reader.Attr("service"),
        };

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

            if (reader.LocalName == "entries")
            {
                response.Entries = reader.ReadInt();
            }
            else
            {
                reader.Skip();
            }
        }

        return response;
    }

    public override string ToString()
    {
        return $"{Service} {Entries}";
    }
}
