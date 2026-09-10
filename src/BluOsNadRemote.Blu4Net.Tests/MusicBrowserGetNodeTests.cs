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
/// Integration tests for <see cref="MusicBrowser.GetAlbumNode"/> /
/// <see cref="MusicBrowser.GetArtistNode"/> against a local HTTP listener.
/// The methods must browse (via /Browse) with the player-provided key format
/// "{service}:MG/{service}-Album?albumid=…" / "{service}:MG/{service}-Artist?artistid=…"
/// and must surface empty or <error> answers as a <see cref="BluChannelException"/>.
/// </summary>
public sealed class MusicBrowserGetNodeTests : IDisposable
{
    private readonly HttpListener _listener;
    private readonly Task _server;
    private readonly Uri _endpoint;
    private readonly Dictionary<string, (int status, string body)> _handlers = new(StringComparer.Ordinal);
    private readonly List<(string key, string query)> _browseRequests = new();
    private readonly object _gate = new();

    public MusicBrowserGetNodeTests()
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
    public async Task GetAlbumNode_BrowsesWithPlayerProvidedKeyFormat()
    {
        // The player-provided key format (lowercase 'f' separator, as the player delivers it).
        const string key = "Tidal:MG/Tidal-Album?albumid=256734465";
        Route(key, (200, """
            <browse type="tracks" serviceName="Tidal" searchKey="Tidal:Search">
              <item text="Shape of You" type="track" playURL="/Add?playnow=1&amp;file=Tidal:68883384"/>
            </browse>
        """));

        var browser = CreateBrowser();
        var node = await browser.GetAlbumNode("Tidal", "256734465");

        Assert.Equal("Tidal", node.ServiceName);
        Assert.Single(node.Entries);
        Assert.Equal("Shape of You", node.Entries.First().Name);

        // Exactly one request, using the player-provided key format.
        Assert.Equal(new[] { (key, string.Empty) }, Requests());
    }

    [Fact]
    public async Task GetArtistNode_BrowsesWithPlayerProvidedKeyFormat()
    {
        const string key = "Tidal:MG/Tidal-Artist?artistid=17356";
        Route(key, (200, """
            <browse type="menu" serviceName="Tidal" searchKey="Tidal:Search">
              <item text="Albums" browseKey="Tidal:Album/%2FAlbums%3Fartistid=17356&amp;service=Tidal" type="link"/>
            </browse>
        """));

        var browser = CreateBrowser();
        var node = await browser.GetArtistNode("Tidal", "17356");

        Assert.Equal("Tidal", node.ServiceName);
        Assert.Single(node.Entries);

        Assert.Equal(new[] { (key, string.Empty) }, Requests());
    }

    [Fact]
    public async Task GetAlbumNode_UnknownId_ReturnsEmptyNodeWithoutError()
    {
        // The player answers a valid but empty <browse> element for unknown ids (observed).
        const string key = "Tidal:MG/Tidal-Album?albumid=0000000000000";
        Route(key, (200, """<browse type="tracks" serviceName="Tidal"/>"""));

        var browser = CreateBrowser();
        var node = await browser.GetAlbumNode("Tidal", "0000000000000");

        Assert.Empty(node.Entries);
        Assert.Empty(node.Categories);
    }

    [Fact]
    public async Task GetAlbumNode_EmptyBody_ThrowsBluChannelExceptionWithHttpStatus()
    {
        // The deprecated firmware behaviour: HTTP 400 and an empty body (no XmlException).
        Route("Tidal:MG/Tidal-Album?albumid=1", (400, string.Empty));

        var browser = CreateBrowser();
        var exception = await Assert.ThrowsAsync<BluChannelException>(() =>
            browser.GetAlbumNode("Tidal", "1"));

        Assert.Contains("400", exception.Message);
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAlbumNode_EmptyBodyWith200_ThrowsBluChannelException()
    {
        Route("Tidal:MG/Tidal-Album?albumid=1", (200, string.Empty));

        var browser = CreateBrowser();
        var exception = await Assert.ThrowsAsync<BluChannelException>(() =>
            browser.GetAlbumNode("Tidal", "1"));

        Assert.Contains("200", exception.Message);
    }

    [Fact]
    public async Task GetAlbumNode_PlayerErrorRoot_ThrowsWithPlayerMessage()
    {
        // An <error> root with a body keeps the existing behaviour (player-provided message).
        Route("Tidal:MG/Tidal-Album?albumid=1", (200, """<error service="Tidal"><message>Not available</message></error>"""));

        var browser = CreateBrowser();
        var exception = await Assert.ThrowsAsync<BluChannelException>(() =>
            browser.GetAlbumNode("Tidal", "1"));

        Assert.Equal("Not available", exception.Message);
    }

    private MusicBrowser CreateBrowser()
    {
        var channel = new BluChannel(_endpoint, CultureInfo.InvariantCulture);
        return new MusicBrowser(channel, ReadBrowse("""<browse/>"""));
    }

    private void Route(string key, (int status, string body) response)
    {
        _handlers[key] = response;
    }

    private static BrowseContentResponse ReadBrowse(string xml)
    {
        using var reader = Fixture.CreateReader(xml);
        return BrowseContentResponse.Read(reader);
    }

    private List<(string key, string query)> Requests()
    {
        lock (_gate)
        {
            return _browseRequests.ToList();
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

            lock (_gate)
            {
                _browseRequests.Add((key, query));
            }

            var (status, body) = _handlers.TryGetValue(key, out var response)
                ? response
                : (404, string.Empty);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/xml";
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(body));
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
