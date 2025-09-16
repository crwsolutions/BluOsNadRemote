using BluOsNadRemote.Blu4Net.Channel;
using System;

namespace BluOsNadRemote.Blu4Net;

public class PlayQueueSong
{
    public int ID { get; }
    public string Artist { get; }
    public string Album { get; }
    public string Title { get; }
    public string TrackstationID { get; }
    public string SongID { get; }
    public string SimilarstationID { get; }
    public string AlbumID { get; }
    public string ArtistID { get; }
    public string Service { get; }

    public PlayQueueSong(PlaylistResponse.Song response)
    {
        ArgumentNullException.ThrowIfNull(response);

        ID = response.ID;
        TrackstationID = response.TrackstationID;
        SongID = response.SongID;
        SimilarstationID = response.SimilarstationID;
        AlbumID = response.AlbumID;
        ArtistID = response.ArtistID;
        Service = response.Service;
        Artist = response.Artist;
        Album = response.Album;
        Title = response.Title;
    }

    public override string ToString()
    {
        return $"{Artist} | {Album} | {Title}";
    }
}
