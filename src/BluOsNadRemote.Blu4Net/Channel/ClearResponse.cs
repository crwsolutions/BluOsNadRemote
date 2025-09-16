﻿using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel
{
    [XmlRoot("playlist")]
    public class ClearResponse
    {
        [XmlAttribute("length")]
        public int Length;

        [XmlAttribute("modified")]
        public int Modified;

        public override string ToString()
        {
            return Length.ToString();
        }
    }
}
