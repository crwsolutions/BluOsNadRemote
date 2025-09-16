﻿using BluOsNadRemote.Blu4Net.Channel;
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

    public MusicContentNode Parent { get; }
    public string ServiceName { get; }
    public Uri ServiceIconUri { get; }
    public IReadOnlyCollection<MusicContentEntry> Entries { get; }
    public IReadOnlyCollection<MusicContentCategory> Categories { get; }

    internal MusicContentNode(BluChannel channel, MusicContentNode parent, BrowseContentResponse response)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Parent = parent;
        ArgumentNullException.ThrowIfNull(response);

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
            throw new NotSupportedException("Musicsource is not searchable");

        var response = await _channel.BrowseContent(_searchKey, searchTerm).ConfigureAwait(false);
        return new MusicContentNode(_channel, this, response);
    }

    public bool HasNext
    {
        get { return _nextKey != null; }
    }

    public async Task<MusicContentNode> ResolveNext()
    {
        var response = await _channel.BrowseContent(_nextKey).ConfigureAwait(false);
        return new MusicContentNode(_channel, this, response);
    }

    public override string ToString()
    {
        return ServiceName;
    }
}
