namespace BluOsNadRemote.App.Models;

public sealed class EndPoint
{
    public EndPoint(Uri uri, string lastKnowName)
    {
        Uri = uri;
        LastKnowName = lastKnowName;
    }

    public EndPoint(string uri, string lastKnowName)
    {
        Uri = new Uri(uri, UriKind.RelativeOrAbsolute);
        LastKnowName = lastKnowName;
    }

    public Uri Uri { get; set; }

    public string LastKnowName { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is EndPoint endPoint)
        {
            return Uri.Equals(endPoint.Uri);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Uri.GetHashCode();
    }
}
