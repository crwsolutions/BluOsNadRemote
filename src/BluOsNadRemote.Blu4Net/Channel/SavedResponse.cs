using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class SavedResponse
{
    public int Entries;

    internal static SavedResponse Read(XmlReader reader)
    {
        reader.ReadRoot("saved");
        var response = new SavedResponse();

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
        return Entries.ToString();
    }
}
