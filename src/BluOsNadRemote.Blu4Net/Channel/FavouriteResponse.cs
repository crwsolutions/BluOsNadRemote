using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

/// <summary>
/// Response to <c>/AddFavourite</c> and <c>/DeleteFavourite</c>, e.g.
/// <c>&lt;favourite service="TuneIn"&gt;deleted&lt;/favourite&gt;</c>.
/// </summary>
public sealed class FavouriteResponse
{
    public string Service;

    public string Text;

    internal static FavouriteResponse Read(XmlReader reader)
    {
        reader.ReadRoot("favourite");
        return new FavouriteResponse
        {
            Service = reader.Attr("service"),
            Text = reader.ReadText(),
        };
    }

    public override string ToString()
    {
        return Text;
    }
}
