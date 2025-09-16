using BluOsNadRemote.Blu4Net.Channel;
using System;

namespace BluOsNadRemote.Blu4Net;

public class PlayerStatus
{
    internal PlayerStatus(StatusResponse response, Uri Endpoint)
    {
        State = BluParser.ParseState(response.State);
        Volume = new Volume(response);
        Media = new PlayerMedia(response, Endpoint); ;
        Position = new PlayPosition(response);
        Shuffle = (ShuffleMode)response.Shuffle;
        Repeat = (RepeatMode)response.Repeat;
    }

    public PlayerState State { get; }
    public Volume Volume { get; }
    public PlayerMedia Media { get; }
    public PlayPosition Position { get; }
    public ShuffleMode Shuffle { get; }
    public RepeatMode Repeat { get; }
}