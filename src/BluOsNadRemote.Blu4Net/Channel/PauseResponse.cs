﻿using System.Xml.Serialization;

namespace Blu4Net.Channel
{
    [XmlRoot("state")]
    public class PauseResponse
    {
        [XmlText()]
        public string State;

        public override string ToString()
        {
            return State;
        }
    }
}
