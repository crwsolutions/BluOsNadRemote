using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class DeleteResponse
{
    public int ID;

    internal static DeleteResponse Read(XmlReader reader)
    {
        reader.ReadRoot("deleted");
        return new DeleteResponse
        {
            ID = reader.ReadInt(),
        };
    }

    public override string ToString()
    {
        return ID.ToString();
    }
}
