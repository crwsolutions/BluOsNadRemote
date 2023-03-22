using Blu4Net;

namespace BluOsNadRemote.App.Extensions;

public static class MusicBrowserExtension
{
    public static async Task<MusicContentNode> GetNodeAlbumNode(this MusicBrowser musicBrowser, string service, string albumID)
    {
        return await musicBrowser.BrowseContent($"/Albums?service={service}&albumid={albumID}");
    }

    public static async Task<MusicContentNode> GetNodeArtistNode(this MusicBrowser musicBrowser, string service, string artistID)
    {
        return await musicBrowser.BrowseContent($"/Artists?service={service}&artistid={artistID}");
    }
}
