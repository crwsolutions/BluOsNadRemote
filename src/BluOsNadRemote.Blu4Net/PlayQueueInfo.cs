using BluOsNadRemote.Blu4Net.Channel;
using System;

namespace BluOsNadRemote.Blu4Net;

public class PlayQueueInfo
{
    public string Name { get; private set; }
    public int Length { get; private set; }

    public PlayQueueInfo(PlaylistResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Name = response.Name;
        Length = response.Length;
    }

    public override string ToString()
    {
        return Name;
    }
}
