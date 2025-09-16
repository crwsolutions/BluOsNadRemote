using BluOsNadRemote.Blu4Net.Channel;
using System;

namespace BluOsNadRemote.Blu4Net;

public sealed class PlayerPreset
{
    public int Number { get; }
    public string Name { get; }
    public Uri ImageUri { get; }

    internal PlayerPreset(PresetsResponse.Preset response, Uri endpoint)
    {
        Number = response.ID;
        Name = response.Name;
        ImageUri = BluParser.ParseAbsoluteUri(response.Image, endpoint);
    }

    public override string ToString()
    {
        return $"{Number}. {Name}";
    }
}
