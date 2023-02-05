
namespace BluOsNadRemote.App;

internal static class Preferences
{
    private const string ENDPOINT = "endpoint";
    public static string Endpoint
    {
        get { return Microsoft.Maui.Storage.Preferences.Default.Get<string>(ENDPOINT, null); }
        set { Microsoft.Maui.Storage.Preferences.Default.Set(ENDPOINT, value); }
    }
}
