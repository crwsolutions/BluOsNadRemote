using BluOsNadRemote.Blu4Net.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BluOsNadRemote.Blu4Net;

public class MusicContentNode
{
    private readonly BluChannel _channel;
    private readonly string _searchKey;
    private readonly string _nextKey;

    // The browse key (and search query) that produced this node; null for the root listing.
    private readonly string _key;
    private readonly string _query;

    public MusicContentNode Parent { get; }
    public string ServiceName { get; }
    public Uri ServiceIconUri { get; }
    public IReadOnlyCollection<MusicContentEntry> Entries { get; }
    public IReadOnlyCollection<MusicContentCategory> Categories { get; }

    internal MusicContentNode(BluChannel channel, MusicContentNode parent, BrowseContentResponse response, string key = null, string query = null)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Parent = parent;
        ArgumentNullException.ThrowIfNull(response);

        _key = key;
        _query = query;
        _searchKey = response.SearchKey;
        _nextKey = string.IsNullOrEmpty(response.NextKey) ? null : response.NextKey;

        ServiceName = response.ServiceName;
        ServiceIconUri = BluParser.ParseAbsoluteUri(response.ServiceIcon, _channel.Endpoint);
        Entries = response.Items != null ? response.Items.Select(element => new MusicContentEntry(channel, this, element)).ToArray() : new MusicContentEntry[0];
        Categories = response.Categories != null ? response.Categories.Select(category => new MusicContentCategory(channel, this, category)).ToArray() : new MusicContentCategory[0];
    }

    public bool IsSearchable
    {
        get { return _searchKey != null; }
    }

    public async Task<MusicContentNode> Search(string searchTerm)
    {
        if (_searchKey == null)
        {
            throw new NotSupportedException("Musicsource is not searchable");
        }

        var response = await _channel.BrowseContent(_searchKey, searchTerm).ConfigureAwait(false);
        return new MusicContentNode(_channel, this, response, _searchKey, searchTerm);
    }

    public bool HasNext
    {
        get { return _nextKey != null; }
    }

    public async Task<MusicContentNode> ResolveNext()
    {
        var response = await _channel.BrowseContent(_nextKey).ConfigureAwait(false);
        return new MusicContentNode(_channel, this, response, _nextKey);
    }

    /// <summary>
    /// Re-resolves this node from the browse key that produced it, e.g. after a
    /// context-menu action changed the player state (added/removed a favourite).
    /// The root listing is re-browsed without a key. The returned node keeps
    /// the same <see cref="Parent"/> chain.
    /// </summary>
    /// <exception cref="InvalidOperationException">The node is not the root listing and has no browse key.</exception>
    public async Task<MusicContentNode> RefreshAsync()
    {
        if (Parent == null)
        {
            // Root listing (MusicBrowser): re-browse without a key.
            var content = await _channel.BrowseContent().ConfigureAwait(false);
            return new MusicBrowser(_channel, content);
        }

        if (_key == null)
        {
            throw new InvalidOperationException("This node has no browse key and cannot be refreshed");
        }

        var response = await _channel.BrowseContent(_key, _query).ConfigureAwait(false);
        return new MusicContentNode(_channel, Parent, response, _key, _query);
    }

    public override string ToString()
    {
        return ServiceName;
    }
}
