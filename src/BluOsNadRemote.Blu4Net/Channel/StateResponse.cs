using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel
{
    [XmlRoot("state")]
    public class StateResponse
    {
        [XmlText()]
        public string State;

        public override string ToString()
        {
            return $"State: {State}";
        }
    }
}
