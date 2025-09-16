﻿using System.Xml.Serialization;

namespace BluOsNadRemote.Blu4Net.Channel
{
    [XmlRoot("playlist")]
    public class RepeatResponse
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlAttribute("length")]
        public int Length;

        [XmlAttribute("modified")]
        public int Modified;

        [XmlAttribute("repeat")]
        public int Repeat;

        public override string ToString()
        {
            return Repeat.ToString();
        }
    }
}
