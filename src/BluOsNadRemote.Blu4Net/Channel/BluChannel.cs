using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

// https://nadelectronics.com/wp-content/uploads/2019/09/Custom-Integration-API-v1.0.pdf
public sealed class BluChannel
{
#if FILE_LOGGING
    int counter = 0;
#endif
    public Uri Endpoint { get; }
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);
    public TimeSpan RetryDelay { get; } = TimeSpan.FromSeconds(5);
    public CultureInfo AcceptLanguage { get; set; }
    public TextWriter Log { get; set; }
    public IObservable<StatusResponse> StatusChanges { get; }
    public IObservable<SyncStatusResponse> SyncStatusChanges { get; }

    public BluChannel(Uri endpoint, CultureInfo acceptLanguage)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

        AcceptLanguage = acceptLanguage ?? throw new ArgumentNullException(nameof(acceptLanguage));

        // recommended long polling interval for Status is 100 seconds
        StatusChanges = LongPolling(StatusResponse.Read, "Status", 100).Retry(RetryDelay).Publish().RefCount();

        // recommended long polling interval for SyncStatus changes is 180 seconds
        SyncStatusChanges = LongPolling(SyncStatusResponse.Read, "SyncStatus", 180).Retry(RetryDelay).Publish().RefCount();
    }

    private void LogMessage(string message)
    {
        if (Log != null && message != null)
        {
            Log.WriteLine(message);
        }
    }

    private async Task<string> SendRequest(Uri requestUri, TimeSpan timeout, CancellationToken cancellationToken)
    {
        LogMessage($"Request: {requestUri}");

        using var client = new HttpClient() { Timeout = timeout };
        client.DefaultRequestHeaders.AcceptLanguage.TryParseAdd(AcceptLanguage.Name);

        using var response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        var xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        LogMessage(xml);

#if FILE_LOGGING
        System.IO.File.WriteAllText(@$"D:\Temp\{Interlocked.Increment(ref counter)}.txt", xml);
#endif

        return xml;
    }

    private async Task<T> SendRequest<T>(Func<XmlReader, T> deserialize, Uri requestUri, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var xml = await SendRequest(requestUri, timeout, cancellationToken);

        using var reader = XmlReader.Create(new StringReader(xml));
        reader.MoveToContent();

        if (reader.LocalName == "error")
        {
            // The player can answer any endpoint with an <error> root (e.g.
            // <error service="Airable"><message>Login to use favourites</message></error>);
            // surface it as a BluChannelException with the player-provided message.
            throw BluChannelException.ParseError(reader);
        }

        return deserialize(reader);
    }

    private Task<T> SendRequest<T>(Func<XmlReader, T> deserialize, Uri requestUri)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        return SendRequest(deserialize, requestUri, Timeout, CancellationToken.None);
    }

    private async Task<T> SendRequest<T>(Func<XmlReader, T> deserialize, string request, NameValueCollection parameters, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = new UriBuilder(Endpoint)
        {
            Path = request,
            Query = parameters != null && parameters.Count > 0 ? parameters.ToString() : null,
        }.Uri;

        return await SendRequest(deserialize, requestUri, timeout, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> SendRequest<T>(Func<XmlReader, T> deserialize, string request, NameValueCollection parameters, CancellationToken cancellationToken)
    {
        return await SendRequest(deserialize, request, parameters, Timeout, cancellationToken).ConfigureAwait(false);
    }

    private Task<T> SendRequest<T>(Func<XmlReader, T> deserialize, string request, NameValueCollection parameters = null)
    {
        return SendRequest(deserialize, request, parameters, Timeout, CancellationToken.None);
    }

    private IObservable<T> LongPolling<T>(Func<XmlReader, T> deserialize, string request, int timeout) where T : ILongPollingResponse
    {
        ArgumentNullException.ThrowIfNull(request);
        if (timeout < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Value must be greater than zero");
        }

        return Observable.Create<T>((observer, cancellationToken) =>
        {
            return Task.Run(async () =>
            {
                var longPollingTag = default(string);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var parameters = HttpUtility.ParseQueryString(string.Empty);
                    if (longPollingTag != null)
                    {
                        parameters["timeout"] = timeout.ToString();
                        parameters["etag"] = longPollingTag.ToString();
                    }
                    try
                    {
                        var response = await SendRequest(deserialize, request, parameters, TimeSpan.FromSeconds(1.5 * timeout), cancellationToken).ConfigureAwait(false);

                        if (longPollingTag != null)
                        {
                            observer.OnNext(response);
                        }
                        longPollingTag = response.ETag;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        observer.OnCompleted();
                        break;
                    }
                    catch (Exception error)
                    {
                        observer.OnError(error);
                        break;
                    }
                }

            }, cancellationToken);
        });
    }


    public async Task<StatusResponse> GetStatus()
    {
        return await SendRequest(StatusResponse.Read, "Status").ConfigureAwait(false);
    }

    public async Task<SyncStatusResponse> GetSyncStatus()
    {
        return await SendRequest(SyncStatusResponse.Read, "SyncStatus").ConfigureAwait(false);
    }

    public Task<StateResponse> Play()
    {
        return SendRequest(StateResponse.Read, "Play");
    }

    public Task<StateResponse> Play(int seek)
    {
        if (seek < 0)
        {
            throw new ArgumentException(nameof(seek), "Value must be greater than zero");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["seek"] = seek.ToString();
        return SendRequest(StateResponse.Read, "Play", parameters);
    }

    public Task<AddSlaveResponse> AddSlave(string address, int port, bool createStereoPair, ChannelMode slaveChannel, string groupName)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(address), "Value cannot be an empty string");
        }

        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Value must be between 1 and 65535");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["slave"] = address;
        parameters["port"] = port.ToString();

        if (createStereoPair)
        {
            parameters["channelMode"] = slaveChannel == ChannelMode.Left ? ChannelMode.Right.ToString().ToLower() : ChannelMode.Left.ToString().ToLower();
            parameters["slaveChannelMode"] = slaveChannel.ToString().ToLower();
            if (string.IsNullOrWhiteSpace(groupName) == false)
            {
                parameters["group"] = groupName;
            }
        }


        return SendRequest(AddSlaveResponse.Read, "AddSlave", parameters);
    }

    public Task<SyncStatusResponse> RemoveSlave(string address, int port)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(address), "Value cannot be an empty string");
        }

        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Value must be between 1 and 65535");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["slave"] = address;
        parameters["port"] = port.ToString();

        return SendRequest(SyncStatusResponse.Read, "RemoveSlave", parameters);
    }

    public Task<SyncStatusResponse> ZoneUngroup(string zoneUngroupUrl)
    {
        return SendRequest(SyncStatusResponse.Read, zoneUngroupUrl);
    }

    public Task<StateResponse> PlayByID(int id)
    {
        if (id < 0)
        {
            throw new ArgumentException(nameof(id), "Value must be greater than zero");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["id"] = id.ToString();
        return SendRequest(StateResponse.Read, "Play", parameters);
    }

    public async Task<StateResponse> Pause(int toggle = 0)
    {
        if (toggle < 0 || toggle > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(toggle), "toggle must be 0 or 1");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        if (toggle == 1)
        {
            parameters["toggle"] = toggle.ToString();
        }

        return await SendRequest(StateResponse.Read, "Pause", parameters).ConfigureAwait(false);
    }

    public async Task<StateResponse> Stop()
    {
        return await SendRequest(StateResponse.Read, "Stop").ConfigureAwait(false);
    }

    public async Task<IdResponse> Skip()
    {
        return await SendRequest(IdResponse.Read, "Skip").ConfigureAwait(false);
    }

    public async Task<IdResponse> Back()
    {
        return await SendRequest(IdResponse.Read, "Back").ConfigureAwait(false);
    }

    public async Task<VolumeResponse> GetVolume()
    {
        return await SendRequest(VolumeResponse.Read, "Volume").ConfigureAwait(false);
    }

    public async Task<VolumeResponse> SetVolume(int percentage)
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Value must be between 0 and 100");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["level"] = percentage.ToString();
        return await SendRequest(VolumeResponse.Read, "Volume", parameters).ConfigureAwait(false);
    }

    public async Task<VolumeResponse> Mute(int mute = 1)
    {
        if (mute < 0 || mute > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mute), "Value must be 0 or 1");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["mute"] = mute.ToString();
        return await SendRequest(VolumeResponse.Read, "Volume", parameters).ConfigureAwait(false);
    }

    public async Task<PlaylistResponse> GetPlaylistStatus()
    {
        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["length"] = 1.ToString();
        return await SendRequest(PlaylistResponse.Read, "Playlist", parameters).ConfigureAwait(false);
    }

    private async Task<PlaylistResponse> GetPlaylist(int startIndex, int length)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Value must be greater than zero");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Value must be greater than zero");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        if (startIndex > 0 || length != int.MaxValue)
        {
            parameters["start"] = startIndex.ToString();
            parameters["end"] = (startIndex + length - 1).ToString();
        }

        return await SendRequest(PlaylistResponse.Read, "Playlist", parameters).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<PlaylistResponse> GetPlaylistPaged(int pageSize)
    {
        var startIndex = 0;

        while (true)
        {
            var listing = await GetPlaylist(startIndex, pageSize).ConfigureAwait(false);
            if (listing.Songs.Length == 0)
            {
                break;
            }

            yield return listing;
            startIndex += listing.Songs.Length;
        }
    }

    public Task<PlaylistResponse> GetPlaylist()
    {
        return GetPlaylist(0, int.MaxValue);
    }

    public async Task<PlaylistResponse> Clear()
    {
        return await SendRequest(PlaylistResponse.Read, "Clear").ConfigureAwait(false);
    }

    public Task<DeleteResponse> Delete(int id)
    {
        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["id"] = id.ToString();
        return SendRequest(DeleteResponse.Read, "Delete", parameters);
    }

    public Task<SavedResponse> Save(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Value cannot be an empty string");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["name"] = name.ToString();
        return SendRequest(SavedResponse.Read, "Save", parameters);
    }

    public Task<PlaylistResponse> GetShuffle()
    {
        return SendRequest(PlaylistResponse.Read, "Shuffle");
    }

    public Task<PlaylistResponse> SetShuffle(int state = 1)
    {
        if (state < 0 || state > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(state), "Value must be 0 or 1");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["state"] = state.ToString();
        return SendRequest(PlaylistResponse.Read, "Shuffle", parameters);
    }

    public Task<PlaylistResponse> GetRepeat()
    {
        return SendRequest(PlaylistResponse.Read, "Repeat");
    }

    public Task<PlaylistResponse> SetRepeat(int state)
    {
        if (state < 0 || state > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(state), "Value must be 0, 1 or 2");
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["state"] = state.ToString();
        return SendRequest(PlaylistResponse.Read, "Repeat", parameters);
    }

    public Task<PresetsResponse> GetPresets()
    {
        return SendRequest(PresetsResponse.Read, "Presets");
    }

    public async Task<LoadedResponse> LoadPreset(int id)
    {
        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["id"] = id.ToString();

        return await SendRequest(LoadedResponse.Read, "Preset", parameters).ConfigureAwait(false);
    }

    public async Task<object> PlayURL(string playURL)
    {
        static object ReadPlayURL(XmlReader reader) => reader.LocalName switch
        {
            "loaded" => PlaylistLoadedResponse.Read(reader),
            "playlist" => PlaylistResponse.Read(reader),
            "state" => StateResponse.Read(reader),
            "addsong" => AddSongResponse.Read(reader),
            // /SetPreset (context menu "Add preset") answers with the updated preset list
            "presets" => PresetsResponse.Read(reader),
            // /AddFavourite and /DeleteFavourite answer with <favourite> (e.g. "deleted")
            "favourite" => FavouriteResponse.Read(reader),
            _ => throw new InvalidOperationException($"Encountered invalid xml root element <{reader.LocalName}>")
        };

        Uri uri;

        if (Uri.TryCreate(playURL, UriKind.Absolute, out var absoluteUri) && absoluteUri.Host.Length > 0)
        {
            uri = absoluteUri;
        }
        else
        {
            uri = new Uri(Endpoint, playURL);
        }

        return await SendRequest(ReadPlayURL, uri).ConfigureAwait(false);
    }

    public Task<ActionResponse> ActionURL(string actionURL)
    {
        return SendRequest(ActionResponse.Read, new Uri(actionURL));
    }

    public Task<BrowseContentResponse> BrowseContent(string key = null, string query = null)
    {
        var parameters = HttpUtility.ParseQueryString(string.Empty);
        if (key != null)
        {
            parameters["key"] = key;

            if (query != null)
            {
                parameters["q"] = query;
            }
        }

        return SendRequest(BrowseContentResponse.Read, "Browse", parameters);
    }
}
