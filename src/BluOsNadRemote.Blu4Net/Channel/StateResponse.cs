using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class StateResponse
{
    public string State;

    internal static StateResponse Read(XmlReader reader)
    {
        reader.ReadRoot("state");
        return new StateResponse
        {
            State = reader.ReadText(),
        };
    }

    public override string ToString()
    {
        return $"State: {State}";
    }
}
