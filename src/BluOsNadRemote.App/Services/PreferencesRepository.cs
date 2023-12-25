using BluOsNadRemote.App.Models;

namespace BluOsNadRemote.App.Services;

public sealed class PreferencesRepository
{
    private const string ENDPOINT = "endpoint";
    public Endpoint SelectedEndpoint
    {
        get
        {
            var uri = Preferences.Default.Get<string>(ENDPOINT, null);
            return uri is null ? null : new Endpoint { Uri = new Uri(uri, UriKind.RelativeOrAbsolute) };
        }
    }

    public void SetEndpoint(string uri)
    {
        if (uri != null)
        { 
            _ = new Uri(uri);
        }
        Preferences.Default.Set(ENDPOINT, uri);
    }

    public void SetEndpoint(Uri uri) => Preferences.Default.Set(ENDPOINT, uri.ToString());
}
