using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class IdResponse
{
    public int ID;

    internal static IdResponse Read(XmlReader reader)
    {
        reader.ReadRoot("id");
        return new IdResponse
        {
            ID = reader.ReadInt(),
        };
    }

    public override string ToString()
    {
        return ID.ToString();
    }
}
