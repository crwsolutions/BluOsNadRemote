﻿using System.Xml.Serialization;

namespace Blu4Net.Channel
{
    [XmlRoot("playlist")]
    public class PlaylistResponse
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlAttribute("length")]
        public int Length;

        [XmlAttribute("modified")]
        public int Modified;

        [XmlElement("song")]
        public Song[] Songs = new Song[0];

        public override string ToString()
        {
            return Name;
        }


        [XmlRoot("song")]
        public class Song
        {
            [XmlAttribute("id")]
            public int ID;

            [XmlAttribute("trackstationid")]
            public string TrackstationID;

            [XmlAttribute("songid")]
            public string SongID;

            [XmlAttribute("similarstationid")]
            public string SimilarstationID;

            [XmlAttribute("albumid")]
            public string AlbumID;

            [XmlAttribute("artistid")]
            public string ArtistID;

            [XmlAttribute("service")]
            public string Service;

            [XmlElement("title")]
            public string Title;

            [XmlElement("art")]
            public string Artist;

            [XmlElement("alb")]
            public string Album;

            public override string ToString()
            {
                return Title;
            }
        }
    }
}
