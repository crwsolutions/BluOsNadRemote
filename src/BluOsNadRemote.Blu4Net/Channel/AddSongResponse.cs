using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class AddSongResponse : LoadedResponse
{
    public int ID;

    public int Count;

    public int Length;

    new internal static AddSongResponse Read(XmlReader reader)
    {
        reader.ReadRoot("addsong");
        return new AddSongResponse
        {
            ID = reader.AttrInt("id"),
            Count = reader.AttrInt("count"),
            Length = reader.AttrInt("length"),
        };
    }

    public override string ToString()
    {
        return ID.ToString();
    }
}
