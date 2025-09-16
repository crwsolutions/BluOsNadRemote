using BluOsNadRemote.Blu4Net.Channel;
using System;

namespace BluOsNadRemote.Blu4Net;

public sealed class PlayPosition
{
    public TimeSpan Elapsed { get; private set; }
    public TimeSpan? Length { get; private set; }

    internal PlayPosition(StatusResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Elapsed = TimeSpan.FromSeconds(response.Seconds);
        Length = response.TotalLength != 0 ? TimeSpan.FromSeconds(response.TotalLength) : default(TimeSpan?);
    }

    public override string ToString()
    {
        return Length != null ? $"{Elapsed} - {Length}" : $"{Elapsed}";
    }
}
