﻿using BluOsNadRemote.Blu4Net.Channel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BluOsNadRemote.Blu4Net;

public sealed class MusicContentEntry
{
    private readonly BluChannel _channel;
    private readonly string _key;
    private readonly string _contextMenuKey;

    public MusicContentNode Node { get; }
    public string Name { get; }
    public string Text2 { get; }
    public string PlayURL { get; }
    public string AutoplayURL { get; }
    public string ActionURL { get; }
    public string Type { get; }
    public Uri ImageUri { get; }

    internal MusicContentEntry(BluChannel channel, MusicContentNode node, BrowseContentResponse.Item item)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Node = node ?? throw new ArgumentNullException(nameof(node));

        ArgumentNullException.ThrowIfNull(item);

        _key = item.BrowseKey;
        _contextMenuKey = item.ContextMenuKey;
        Name = item.Text;
        Text2 = item.Text2;
        PlayURL = item.PlayURL;
        AutoplayURL = item.AutoplayURL;
        ActionURL = item.ActionURL;
        Type = !string.IsNullOrEmpty(item.Type) ? item.Type.First().ToString().ToUpper() + item.Type.Substring(1) : null;
        ImageUri = BluParser.ParseAbsoluteUri(item.Image, channel.Endpoint);
    }

    public bool HasContextMenu
    {
        get { return _contextMenuKey != null; }
    }

    public async Task<MusicContentNode> ResolveContextMenu()
    {
        if (_contextMenuKey == null)
            throw new InvalidOperationException("This entry has no contextMenu");

        var response = await _channel.BrowseContent(_contextMenuKey).ConfigureAwait(false);
        return new MusicContentNode(_channel, Node, response);
    }

    public bool IsResolvable
    {
        get { return _key != null; }
    }

    public async Task<MusicContentNode> Resolve()
    {
        if (_key == null)
            throw new InvalidOperationException("This entry is not resolvable");

        var response = await _channel.BrowseContent(_key).ConfigureAwait(false);
        return new MusicContentNode(_channel, Node, response);
    }

    public override string ToString()
    {
        return Name;
    }
}
