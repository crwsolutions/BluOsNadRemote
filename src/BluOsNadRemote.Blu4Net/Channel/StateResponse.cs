using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel;

[XmlRoot("state")]
public sealed class StateResponse
{
    [XmlText()]
    public string State;

    public override string ToString()
    {
        return $"State: {State}";
    }
}
