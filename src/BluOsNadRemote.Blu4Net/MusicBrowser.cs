using BluOsNadRemote.Blu4Net.Channel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BluOsNadRemote.Blu4Net;

public sealed class MusicBrowser : MusicContentNode
{

    private readonly BluChannel _channel;

    internal MusicBrowser(BluChannel channel, BrowseContentResponse response)
        : base(channel, null, response)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public async Task<MusicContentNode> BrowseContent(string key)
    {
        var content = await _channel.BrowseContent(key).ConfigureAwait(false);
        return new MusicContentNode(_channel, this, content, key);
    }

    /// <summary>
    /// Returns the node of the album with the given <paramref name="albumId"/> (its tracks).
    /// Browses with the <c>{service}:MG/{service}-Album?albumid=…</c> key that the player itself
    /// provides on album items; the hand-built <c>/Albums?service=…&amp;albumid=…</c> browse key is
    /// no longer honored by the firmware (HTTP 400 + empty body).
    /// An unknown album id yields a valid but empty <c>&lt;browse&gt;</c> element.
    /// </summary>
    /// <param name="service">Service name of the album, e.g. "Tidal" (the status/queue <c>service</c> value).</param>
    /// <param name="albumId">Album id (the status/queue <c>albumid</c> value).</param>
    public Task<MusicContentNode> GetAlbumNode(string service, string albumId)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(albumId);

        Debug.WriteLine($"[MusicBrowser] GetAlbumNode: service='{service}', albumId='{albumId}'");
        return GetNodeById("album", service, albumId, "Album", "albumid");
    }

    /// <summary>
    /// Returns the node of the artist with the given <paramref name="artistId"/> (their discography menu).
    /// Browses with the <c>{service}:MG/{service}-Artist?artistid=…</c> key that the player itself
    /// provides on artist items, see <see cref="GetAlbumNode"/>.
    /// An unknown artist id yields a valid but empty <c>&lt;browse&gt;</c> element.
    /// </summary>
    /// <param name="service">Service name of the artist, e.g. "Tidal" (the status/queue <c>service</c> value).</param>
    /// <param name="artistId">Artist id (the status/queue <c>artistid</c> value).</param>
    public Task<MusicContentNode> GetArtistNode(string service, string artistId)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(artistId);

        Debug.WriteLine($"[MusicBrowser] GetArtistNode: service='{service}', artistId='{artistId}'");
        return GetNodeById("artist", service, artistId, "Artist", "artistid");
    }

    // The key mirrors the player-provided browseKey format, e.g. "Tidal:MG/Tidal-Album?albumid=123"
    // (lowercase 'f' query separator, as observed in the keys the player delivers).
    private Task<MusicContentNode> GetNodeById(string kind, string service, string id, string keyKind, string idParameter)
    {
        var key = $"{service}:MG/{service}-{keyKind}?{idParameter}={id}";
        Debug.WriteLine($"[MusicBrowser] {kind}: browsing key '{key}'");
        return BrowseContent(key);
    }

    public Task PlayURL(string playURL)
    {
        return _channel.PlayURL(playURL);
    }

}
