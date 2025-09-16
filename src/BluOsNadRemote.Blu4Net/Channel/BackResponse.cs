using System.Xml.Serialization;

namespace Blu4Net.Channel
{
    [XmlRoot("id")]
    public class BackResponse
    {
        [XmlText()]
        public int ID;

        public override string ToString()
        {
            return ID.ToString();
        }
    }
}
