using BluOsNadRemote.Blu4Net.Channel;
using System;
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
        return new MusicContentNode(_channel, this, content);
    }

    public Task PlayURL(string playURL)
    {
        return _channel.PlayURL(playURL);
    }

}
