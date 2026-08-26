using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BluOsNadRemote.Blu4Net;
using BluOsNadRemote.Blu4Net.Channel;
using Xunit;

namespace BluOsNadRemote.Blu4Net.Tests;

/// <summary>
/// Integration tests for <see cref="MusicContentNode.RefreshAsync"/> against a local HTTP listener.
/// The server returns 1 entry on the first browse of a key and 2 entries (a "new favourite") on the
/// second, so a refresh demonstrably re-fetches the changed player state.
/// </summary>
public sealed class MusicContentNodeRefreshTests : IDisposable
{
    private const string BrowseKey = "Deezer:Album?albumid=123";
    private const string SearchKey = "Deezer:Search";

    private readonly HttpListener _listener;
    private readonly Task _server;
    private readonly Uri _endpoint;
    private readonly Dictionary<string, int> _requestsPerKey = new();
    private readonly List<KeyValuePair<string, string>> _browseRequests = new();
    private readonly object _gate = new();

    public MusicContentNodeRefreshTests()
    {
        _listener = new HttpListener();
        var port = GetFreePort();
        _endpoint = new Uri($"http://127.0.0.1:{port}/");
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        _server = Task.Run(ServerLoop);
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
    }

    [Fact]
    public async Task Refresh_ChildNode_ReusesBrowseKeyAndReturnsUpdatedContent()
    {
        var channel = new BluChannel(_endpoint, CultureInfo.InvariantCulture);
        var browser = new MusicBrowser(channel, ReadBrowse("<browse/>"));

        var node = await browser.BrowseContent(BrowseKey);   // 1st browse of the key: 1 entry
        Assert.Single(node.Entries);

        var refreshed = await node.RefreshAsync();           // 2nd browse of the key: the added favourite

        Assert.Same(browser, refreshed.Parent);
        Assert.Equal(2, refreshed.Entries.Count);
        Assert.Equal("New favourite", refreshed.Entries.ElementAt(1).Name);

        var requests = Requests();
        Assert.Equal(2, requests.Count);
        Assert.Equal(BrowseKey, requests[1].Key);            // the refresh requested the node's own key
    }

    [Fact]
    public async Task Refresh_SearchNode_ReusesSearchKeyAndQuery()
    {
        var channel = new BluChannel(_endpoint, CultureInfo.InvariantCulture);
        var browser = new MusicBrowser(channel, ReadBrowse($"""<browse searchKey="{SearchKey}"/>"""));

        var node = await browser.Search("anne");             // 1st: key=searchKey, q=anne
        Assert.Single(node.Entries);

        var refreshed = await node.RefreshAsync();           // 2nd: same key + query

        Assert.Same(browser, refreshed.Parent);
        Assert.Equal(2, refreshed.Entries.Count);

        var requests = Requests();
        Assert.Equal(2, requests.Count);
        Assert.Equal(SearchKey, requests[1].Key);
        Assert.Equal("anne", requests[1].Value);
    }

    [Fact]
    public async Task Refresh_RootListing_ReBrowsesWithoutKey()
    {
        var channel = new BluChannel(_endpoint, CultureInfo.InvariantCulture);
        var browser = new MusicBrowser(channel, ReadBrowse("<browse><item text=\"Deezer\" type=\"link\"/></browse>"));

        var refreshed = await browser.RefreshAsync();

        Assert.IsType<MusicBrowser>(refreshed);
        var requests = Requests();
        Assert.Single(requests);
        Assert.Equal(string.Empty, requests[0].Key);         // the root listing is re-browsed without a key
    }

    [Fact]
    public async Task Refresh_NodeWithoutKey_Throws()
    {
        var channel = new BluChannel(_endpoint, CultureInfo.InvariantCulture);
        var parent = new MusicContentNode(channel, null, ReadBrowse("<browse/>"));
        var node = new MusicContentNode(channel, parent, ReadBrowse("<browse/>"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => node.RefreshAsync());
    }

    private static BrowseContentResponse ReadBrowse(string xml)
    {
        using var reader = Fixture.CreateReader(xml);
        return BrowseContentResponse.Read(reader);
    }

    private List<KeyValuePair<string, string>> Requests()
    {
        lock (_gate)
        {
            return new List<KeyValuePair<string, string>>(_browseRequests);
        }
    }

    private async Task ServerLoop()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch
            {
                break;
            }

            var parameters = HttpUtility.ParseQueryString(context.Request.Url!.Query);
            var key = parameters["key"] ?? string.Empty;
            var query = parameters["q"] ?? string.Empty;

            var isSecondRequest = false;
            lock (_gate)
            {
                isSecondRequest = _requestsPerKey.TryGetValue(key, out var count) && count >= 1;
                _requestsPerKey[key] = (isSecondRequest ? count : 0) + 1;
                _browseRequests.Add(new KeyValuePair<string, string>(key, query));
            }

            // first browse of a key: 1 entry; the refresh (second browse): the player "added a favourite"
            var xml = isSecondRequest
                ? """<browse><item text="Station 1" type="audio"/><item text="New favourite" type="audio"/></browse>"""
                : """<browse><item text="Station 1" type="audio"/></browse>""";

            var bytes = Encoding.UTF8.GetBytes(xml);
            context.Response.ContentType = "application/xml";
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
