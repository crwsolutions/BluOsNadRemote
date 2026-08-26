using System.Collections.Generic;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public class PlaylistResponse
{
    public string Name;

    public int Length;

    public int Modified;

    public int Shuffle;

    public int Repeat;

    public Song[] Songs = new Song[0];

    internal static PlaylistResponse Read(XmlReader reader)
    {
        reader.ReadRoot("playlist");
        var response = new PlaylistResponse
        {
            Name = reader.Attr("name"),
            Length = reader.AttrInt("length"),
            Modified = reader.AttrInt("modified"),
            Shuffle = reader.AttrInt("shuffle"),
            Repeat = reader.AttrInt("repeat"),
        };

        var songs = new List<Song>();

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    break;
                }
                continue;
            }

            if (reader.LocalName == "song")
            {
                songs.Add(Song.Read(reader));
            }
            else
            {
                reader.Skip();
            }
        }

        response.Songs = songs.ToArray();
        return response;
    }

    public override string ToString()
    {
        return Name;
    }


    public class Song
    {
        public int ID;

        public string TrackstationID;

        public string SongID;

        public string SimilarstationID;

        public string AlbumID;

        public string ArtistID;

        public string Service;

        public string Title;

        public string Artist;

        public string Album;

        internal static Song Read(XmlReader reader)
        {
            var song = new Song
            {
                ID = reader.AttrInt("id"),
                TrackstationID = reader.Attr("trackstationid"),
                SongID = reader.Attr("songid"),
                SimilarstationID = reader.Attr("similarstationid"),
                AlbumID = reader.Attr("albumid"),
                ArtistID = reader.Attr("artistid"),
                Service = reader.Attr("service"),
            };

            if (reader.IsEmptyElement)
            {
                return song;
            }

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        break;
                    }
                    continue;
                }

                if (reader.LocalName == "title")
                {
                    song.Title = reader.ReadText();
                }
                else if (reader.LocalName == "art")
                {
                    song.Artist = reader.ReadText();
                }
                else if (reader.LocalName == "alb")
                {
                    song.Album = reader.ReadText();
                }
                else
                {
                    reader.Skip();
                }
            }

            return song;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
