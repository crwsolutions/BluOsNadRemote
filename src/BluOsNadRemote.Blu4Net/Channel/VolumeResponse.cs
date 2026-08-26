using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public sealed class VolumeResponse : ILongPollingResponse
{
    public string ETag { get; set; }

    public double Decibel;

    public int Mute;

    public int Volume;

    internal static VolumeResponse Read(XmlReader reader)
    {
        reader.ReadRoot("volume");
        return new VolumeResponse
        {
            ETag = reader.Attr("etag"),
            Decibel = reader.AttrDouble("db"),
            Mute = reader.AttrInt("mute"),
            Volume = reader.ReadInt(),
        };
    }

    public override string ToString()
    {
        return $"{Volume}% {Decibel}db";
    }
}
