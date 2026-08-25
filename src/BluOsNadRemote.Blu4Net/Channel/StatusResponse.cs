using System.Collections.Generic;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class StatusResponse : ILongPollingResponse
{
    public ActionsArray Actions = new();

    public string ETag { get; set; }

    public string State;

    public string StreamFormat;

    public string Quality;

    public int Volume;

    public int CanSeek;

    public double Decibel;

    public string Image;

    public string Service;

    public string Artist;

    public string ArtistID;

    public string Album;

    public string AlbumID;

    public string Title1;

    public string Title2;

    public string Title3;

    public int? Song; // can be empty

    public string SongID;

    public string TrackstationID;

    public double TotalLength;

    public int Seconds;

    public int Shuffle;

    public int Repeat;

    public string PlaylistID;

    public string PresetsID;

    public string IsPreset;

    public string PresetName;

    public string StreamUrl;

    public string ServiceIcon;

    internal static StatusResponse Read(XmlReader reader)
    {
        reader.ReadRoot("status");
        var response = new StatusResponse
        {
            ETag = reader.Attr("etag"),
        };

        // ReadText/ReadInt/ReadDouble/ActionsArray.Read leave the reader on the handled
        // element's EndElement, so Read() must not be called again for that iteration —
        // otherwise the next sibling element gets skipped (only occurs in minified XML, where
        // there is no whitespace node between the EndElement and the next sibling).
        var handled = false;
        while (true)
        {
            if (!handled)
            {
                if (!reader.Read())
                {
                    break;
                }
            }
            handled = false;

            if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "status")
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (TryReadKnownElement(reader, response))
            {
                handled = true;
            }
            else
            {
                // undocumented elements may be present and are ignored. Skip() leaves the reader
                // positioned on the next sibling element, so we must not Read() again this pass.
                reader.Skip();
                handled = true;
            }
        }

        return response;
    }

    private static bool TryReadKnownElement(XmlReader reader, StatusResponse response)
    {
        switch (reader.LocalName)
        {
            case "actions":
                response.Actions = ActionsArray.Read(reader);
                return true;
            case "state":
                response.State = reader.ReadText();
                return true;
            case "streamFormat":
                response.StreamFormat = reader.ReadText();
                return true;
            case "quality":
                response.Quality = reader.ReadText();
                return true;
            case "volume":
                response.Volume = reader.ReadInt();
                return true;
            case "canSeek":
                response.CanSeek = reader.ReadInt();
                return true;
            case "db":
                response.Decibel = reader.ReadDouble();
                return true;
            case "image":
                response.Image = reader.ReadText();
                return true;
            case "service":
                response.Service = reader.ReadText();
                return true;
            case "artist":
                response.Artist = reader.ReadText();
                return true;
            case "artistid":
                response.ArtistID = reader.ReadText();
                return true;
            case "album":
                response.Album = reader.ReadText();
                return true;
            case "albumid":
                response.AlbumID = reader.ReadText();
                return true;
            case "title1":
                response.Title1 = reader.ReadText();
                return true;
            case "title2":
                response.Title2 = reader.ReadText();
                return true;
            case "title3":
                response.Title3 = reader.ReadText();
                return true;
            case "song":
                response.Song = reader.ReadIntOrNullOrThrow();
                return true;
            case "songid":
                response.SongID = reader.ReadText();
                return true;
            case "trackstationid":
                response.TrackstationID = reader.ReadText();
                return true;
            case "totlen":
                response.TotalLength = reader.ReadDouble();
                return true;
            case "secs":
                response.Seconds = reader.ReadInt();
                return true;
            case "shuffle":
                response.Shuffle = reader.ReadInt();
                return true;
            case "repeat":
                response.Repeat = reader.ReadInt();
                return true;
            case "pid":
                response.PlaylistID = reader.ReadText();
                return true;
            case "prid":
                response.PresetsID = reader.ReadText();
                return true;
            case "is_preset":
                response.IsPreset = reader.ReadText();
                return true;
            case "preset_name":
                response.PresetName = reader.ReadText();
                return true;
            case "streamUrl":
                response.StreamUrl = reader.ReadText();
                return true;
            case "serviceIcon":
                response.ServiceIcon = reader.ReadText();
                return true;
            default:
                return false;
        }
    }

    public override string ToString()
    {
        return State;
    }

    public sealed class ActionsArray
    {
        public Action[] Items = new Action[0];

        internal static ActionsArray Read(XmlReader reader)
        {
            var actions = new List<Action>();

            if (reader.IsEmptyElement)
            {
                return new ActionsArray();
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

                if (reader.LocalName == "action")
                {
                    actions.Add(Action.Read(reader));
                }
                else
                {
                    reader.Skip();
                }
            }

            return new ActionsArray
            {
                Items = actions.ToArray(),
            };
        }
    }

    public sealed class Action
    {
        public string Icon;

        public string Name;

        public string Notification;

        public string Text;

        public string Url;

        internal static Action Read(XmlReader reader)
        {
            return new Action
            {
                Icon = reader.Attr("icon"),
                Name = reader.Attr("name"),
                Notification = reader.Attr("notification"),
                Text = reader.Attr("text"),
                Url = reader.Attr("url"),
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
