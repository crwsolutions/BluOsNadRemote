using Nad4Net.Extensions;
using Nad4Net.Model;
using PrimS.Telnet;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BclTcpClient = System.Net.Sockets.TcpClient;
using TelnetTcpClient = PrimS.Telnet.TcpClient;

namespace Nad4Net;

public class NadRemote : IDisposable
{
    private CancellationTokenSource? _tokenSource;
    private readonly string[] _sources = new string[10];
    private Client? _client;
    private readonly string _host;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private volatile bool _disposed = false;
    private const int PORT = 23;

    private const string ON = "On";
    private const string OFF = "Off";
    private const string MAIN_MODEL_COMMAND = "Main.Model";
    private const string MAIN_SOURCE_COMMAND = "Main.Source";
    private const string MAIN_AUDIO_CODEC_COMMAND = "Main.Audio.CODEC";
    private const string MAIN_AUDIO_CHANNELS_COMMAND = "Main.Audio.Channels";
    private const string MAIN_AUDIO_RATE_COMMAND = "Main.Audio.Rate";
    private const string MAIN_VIDEO_ARC_COMMAND = "Main.Video.ARC";
    private const string MAIN_LISTENINGMODE_COMMAND = "Main.ListeningMode";
    private const string DIRAC1_STATE_COMMAND = "Dirac1.State";
    private const string DIRAC1_NAME_COMMAND = "Dirac1.Name";
    private const string DIRAC2_STATE_COMMAND = "Dirac2.State";
    private const string DIRAC2_NAME_COMMAND = "Dirac2.Name";
    private const string DIRAC3_STATE_COMMAND = "Dirac3.State";
    private const string DIRAC3_NAME_COMMAND = "Dirac3.Name";
    private const string MAIN_DIRAC_COMMAND = "Main.Dirac";
    private const string MAIN_TRIM_SUB_COMMAND = "Main.Trim.Sub";
    private const string MAIN_TRIM_SURROUND_COMMAND = "Main.Trim.Surround";
    private const string MAIN_TRIM_CENTER_COMMAND = "Main.Trim.Center";
    private const string MAIN_DIMMER_COMMAND = "Main.Dimmer";
    private const string MAIN_POWER_COMMAND = "Main.Power";
    private const string MAIN_DOLBY_DRC_COMMAND = "Main.Dolby.DRC";
    private const string SOURCE_PREFIX_COMMAND = "Source";
    private const char COMMAND_END = '\n';

    /// <summary>Max time to wait for the TCP connect + telnet handshake.</summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(3);
    /// <summary>Max time to wait for a single read of the change-detection loop.</summary>
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(30);
    /// <summary>Delay before the change-detection loop retries after a connection loss.</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
    public IObservable<CommandList> CommandChanges { get; }

    private readonly CommandList _model = new();
    private string? _initialRead;
    public Uri Endpoint { get; }

    private static readonly char[] _equalsSeparator = ['='];

    public NadRemote(Uri endpoint)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _host = endpoint.Host;

        CommandChanges = SetupChangeDetectionLoop().Publish().RefCount();
    }

    public bool IsConnected => _client?.IsConnected is true;

    /// <summary>
    /// Establishes the telnet connection within a bounded timeout.
    /// Throws <see cref="NadConnectException"/> when the endpoint cannot be reached.
    /// </summary>
    public Task ConnectAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        return EnsureConnectedAsync(timeout ?? ConnectTimeout, ct);
    }

    public async Task<string?> PingAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(timeout ?? ConnectTimeout, ct);
        // The initial handshake read (done while connecting) is the proof that the
        // connection works; returning it avoids an extra blocking read.
        return _client?.IsConnected is true ? _initialRead : null;
    }

    public async Task GetCommandListAsync(Action<CommandList> resultHandler)
    {
        await EnsureConnectedAsync(ConnectTimeout);
        await WriteQueryCommandAsync(MAIN_MODEL_COMMAND);
        await WriteQueryCommandAsync(MAIN_SOURCE_COMMAND);
        await WriteQueryCommandAsync(MAIN_AUDIO_CODEC_COMMAND);
        await WriteQueryCommandAsync(MAIN_AUDIO_CHANNELS_COMMAND);
        await WriteQueryCommandAsync(MAIN_AUDIO_RATE_COMMAND);
        await WriteQueryCommandAsync(MAIN_VIDEO_ARC_COMMAND);
        await WriteQueryCommandAsync(MAIN_LISTENINGMODE_COMMAND);
        await WriteQueryCommandAsync(DIRAC1_STATE_COMMAND);
        await WriteQueryCommandAsync(DIRAC1_NAME_COMMAND);
        await WriteQueryCommandAsync(DIRAC2_STATE_COMMAND);
        await WriteQueryCommandAsync(DIRAC2_NAME_COMMAND);
        await WriteQueryCommandAsync(DIRAC3_STATE_COMMAND);
        await WriteQueryCommandAsync(DIRAC3_NAME_COMMAND);
        await WriteQueryCommandAsync(MAIN_DIRAC_COMMAND);
        await WriteQueryCommandAsync(MAIN_TRIM_SUB_COMMAND);
        await WriteQueryCommandAsync(MAIN_TRIM_SURROUND_COMMAND);
        await WriteQueryCommandAsync(MAIN_TRIM_CENTER_COMMAND);
        await WriteQueryCommandAsync(MAIN_DIMMER_COMMAND);
        await WriteQueryCommandAsync(MAIN_POWER_COMMAND);
        await WriteQueryCommandAsync(MAIN_DOLBY_DRC_COMMAND);
        for (var i = 0; i < _sources.Length; i++)
        {
            await WriteQueryCommandAsync($"{SOURCE_PREFIX_COMMAND}{i + 1}.Name");
        }
        Parse(await _client!.ReadAsync(ReadTimeout));
        resultHandler.Invoke(_model);
    }

    public async Task DoSurroundPlusAsync() => await WritePlusCommandAsync(MAIN_TRIM_SURROUND_COMMAND);
    public async Task DoSurroundMinusAsync() => await WriteMinusCommandAsync(MAIN_TRIM_SURROUND_COMMAND);
    public async Task DoSubPlusAsync() => await WritePlusCommandAsync(MAIN_TRIM_SUB_COMMAND);
    public async Task DoSubMinusAsync() => await WriteMinusCommandAsync(MAIN_TRIM_SUB_COMMAND);
    public async Task DoCenterPlusAsync() => await WritePlusCommandAsync(MAIN_TRIM_CENTER_COMMAND);
    public async Task DoCenterMinusAsync() => await WriteMinusCommandAsync(MAIN_TRIM_CENTER_COMMAND);
    public async Task ToggleOnOffAsync() => await WriteSetCommandAsync(MAIN_POWER_COMMAND, _model.MainPower ? OFF : ON);
    public async Task ToggleDimmerAsync() => await WriteSetCommandAsync(MAIN_DIMMER_COMMAND, _model.MainDimmer ? OFF : ON);
    public async Task SetListeningModeAsync(string value) => await WriteSetCommandAsync(MAIN_LISTENINGMODE_COMMAND, value);
    public async Task SetMainDiracAsync(int value) => await WriteSetCommandAsync(MAIN_DIRAC_COMMAND, (value + 1).ToString());

    /// <summary>
    /// Connects when not connected yet, or reconnects when the previous connection was lost.
    /// Only one connect attempt runs at a time. Throws <see cref="NadConnectException"/>
    /// when the endpoint cannot be reached within the timeout.
    /// </summary>
    private async Task EnsureConnectedAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NadRemote));
        }

        lock (_lock)
        {
            if (_client is { IsConnected: true })
            {
                return;
            }
        }

        await _connectGate.WaitAsync(ct);
        try
        {
            lock (_lock)
            {
                // Another caller may have connected while we were waiting for the gate.
                if (_client is { IsConnected: true })
                {
                    return;
                }
            }

            await ConnectFreshAsync(timeout, ct);
        }
        finally
        {
            _connectGate.Release();
        }
    }

    /// <summary>
    /// Disposes any previous connection and opens a new one. The TCP connect itself is bounded
    /// by the timeout (the underlying Telnet library's <c>TcpByteStream(string, int)</c> connects
    /// synchronously with no timeout, so we connect first and hand the socket over).
    /// </summary>
    private async Task ConnectFreshAsync(TimeSpan timeout, CancellationToken ct)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NadRemote));
        }

        Debug.WriteLine($"(Re-)connecting the telnet connection to {_host}:{PORT}");
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            DisposeLocked();
            _tokenSource = new CancellationTokenSource();
            _initialRead = null;
        }

        // Bounds the TCP connect (the library's TcpByteStream(string,int) would otherwise
        // block with no timeout). Linked to our token so a Dispose() also aborts it.
        using var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _tokenSource!.Token);
        connectionCts.CancelAfter(timeout);

        var tcp = new BclTcpClient();
        bool handedOff = false;
        try
        {
            // Connect ourselves (bounded) and hand the connected socket to the library.
            // From the point of wrapping, the wrapper owns the socket (handedOff=true).
            await tcp.ConnectAsync(_host, PORT, connectionCts.Token);

            lock (_lock)
            {
                if (_tokenSource is null || _disposed)
                {
                    // We were disposed while connecting; bail (the finally closes the socket).
                    return;
                }

                _client = new Client(new TcpByteStream(new TelnetTcpClient(tcp)), timeout, _tokenSource.Token);
                handedOff = true;
            }

            // Best-effort initial read: a NAD usually sends a greeting, but a missing banner
            // must not be treated as "not connected" — the TCP connect above is the proof.
            try
            {
                var initial = await _client!.ReadAsync(timeout);
                _initialRead = initial;
                Parse(initial);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // the caller cancelled (e.g. the page went away)
            }
            catch (OperationCanceledException)
            {
                // Our own token was cancelled by a Dispose() while connecting; not a failure
                // the caller needs to see (it is going away).
                Debug.WriteLine("Initial read cancelled (disposing); connection was up");
            }
            catch (Exception readError)
            {
                // The connection dropped during the handshake; treat it as a failed connect
                // so the caller gets the friendly message.
                throw new NadConnectException(_host, TranslateConnectFailure(readError), readError);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // caller cancelled
        }
        catch (NadConnectException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeout (our CancelAfter), socket refused, negotiation failure, ...: normalize
            // to a friendly NadConnectException for the UI.
            throw new NadConnectException(_host, TranslateConnectFailure(ex), ex);
        }
        finally
        {
            // Only close our socket if the library never took ownership of it.
            if (!handedOff)
            {
                tcp.Dispose();
            }
        }
    }

    /// <summary>
    /// Maps a raw socket/telnet failure to a friendly <see cref="NadConnectReason"/>.
    /// </summary>
    private static NadConnectReason TranslateConnectFailure(Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            return NadConnectReason.Timeout;
        }

        return ex is SocketException or IOException
            ? NadConnectReason.Unreachable
            : NadConnectReason.Negotiation;
    }

    /// <summary>
    /// Reads from the connection until it is cancelled or disposed, reconnecting after the
    /// retry delay when the connection is lost (e.g. the NAD goes to sleep or the router
    /// drops it). A quiet NAD produces an empty read, which is ignored.
    /// </summary>
    private IObservable<CommandList> SetupChangeDetectionLoop()
    {
        return Observable.Create<CommandList>((observer, cancellationToken) =>
        {
            return Task.Run(async () =>
            {
                Debug.WriteLine("Starting new telnet listener");
                try
                {
                    while (!cancellationToken.IsCancellationRequested && !_disposed)
                    {
                        try
                        {
                            await EnsureConnectedAsync(ConnectTimeout, cancellationToken);
                            await ConsumeReadsAsync(observer, cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                        catch (Exception error)
                        {
                            // Connect failed (NadConnectException) or the connection was lost
                            // mid-read: back off and try again so the loop self-heals.
                            Debug.WriteLine($"Telnet connection lost, retrying in {RetryDelay.TotalSeconds}s: {error}");
                            if (!await WaitRetryDelayAsync(cancellationToken))
                            {
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    observer.OnCompleted();
                }

                Debug.WriteLine($"Stopped the telnet listener, token: {cancellationToken.IsCancellationRequested}");
            }, cancellationToken);
        });
    }

    /// <summary>
    /// Reads data until the connection is lost, the loop is cancelled, or the remote is
    /// disposed. <see cref="Client.ReadAsync(TimeSpan)"/> returns an empty string when the
    /// NAD is quiet, so a silent NAD does not disturb the loop; only real data is parsed.
    /// </summary>
    private async Task ConsumeReadsAsync(IObserver<CommandList> observer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            var client = _client;
            if (client is null)
            {
                // Disposed between reads.
                break;
            }

            string s;
            try
            {
                s = await client.ReadAsync(ReadTimeout);
            }
            catch (OperationCanceledException) when (_disposed || cancellationToken.IsCancellationRequested)
            {
                // We were disposed (which cancels the client's token) or the caller cancelled.
                break;
            }
            // Any other exception is a lost connection; let it propagate so the outer loop
            // reconnects.

            if (s.Length > 0)
            {
                Parse(s);
                observer.OnNext(_model);
            }
        }
    }

    /// <summary>
    /// Waits the retry delay, returning false when the wait was cancelled/disposed.
    /// </summary>
    private async Task<bool> WaitRetryDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(RetryDelay, cancellationToken);
            return !_disposed;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private void Parse(string s)
    {
        using var strReader = new StringReader(s);
#if DEBUG
        var numLines = s == string.Empty ? 0 : s.Count(c => c.Equals(COMMAND_END)) + 1;
        Debug.WriteLine($"{numLines} properties received");
#endif

        string? line;
        while ((line = strReader.ReadLine()) != null)
        {
            Debug.WriteLine(line);
            var parts = line.Split(_equalsSeparator);
            if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
            {
                Debug.WriteLine($"Skipping malformed line: {line}");
                continue;
            }

            switch (parts[0])
            {
                case MAIN_MODEL_COMMAND:
                    _model.MainModel = parts[1];
                    break;
                case MAIN_SOURCE_COMMAND:
                    _model.MainSource = parts[1];
                    SetModelMainSourceName();
                    break;
                case MAIN_AUDIO_CODEC_COMMAND:
                    _model.MainAudioCODEC = parts[1];
                    break;
                case MAIN_AUDIO_CHANNELS_COMMAND:
                    _model.MainAudioChannels = parts[1];
                    break;
                case MAIN_AUDIO_RATE_COMMAND:
                    _model.MainAudioRate = parts[1];
                    break;
                case MAIN_VIDEO_ARC_COMMAND:
                    _model.MainVideoARC = parts[1];
                    break;
                case MAIN_LISTENINGMODE_COMMAND:
                    _model.MainListeningMode = parts[1];
                    break;
                case MAIN_DIRAC_COMMAND:
                    if (int.TryParse(parts[1], out var dirac))
                    {
                        _model.MainDirac = dirac - 1;
                    }
                    break;
                case DIRAC1_STATE_COMMAND:
                    _model.Dirac1State = parts[1];
                    break;
                case DIRAC1_NAME_COMMAND:
                    _model.Dirac1Name = parts[1];
                    break;
                case DIRAC2_STATE_COMMAND:
                    _model.Dirac2State = parts[1];
                    break;
                case DIRAC2_NAME_COMMAND:
                    _model.Dirac2Name = parts[1];
                    break;
                case DIRAC3_STATE_COMMAND:
                    _model.Dirac3State = parts[1];
                    break;
                case DIRAC3_NAME_COMMAND:
                    _model.Dirac3Name = parts[1];
                    break;
                case MAIN_TRIM_SUB_COMMAND:
                    if (int.TryParse(parts[1], out var sub))
                    {
                        _model.MainTrimSub = sub;
                    }
                    break;
                case MAIN_TRIM_SURROUND_COMMAND:
                    if (int.TryParse(parts[1], out var surround))
                    {
                        _model.MainTrimSurround = surround;
                    }
                    break;
                case MAIN_TRIM_CENTER_COMMAND:
                    if (int.TryParse(parts[1], out var center))
                    {
                        _model.MainTrimCenter = center;
                    }
                    break;
                case MAIN_DIMMER_COMMAND:
                    _model.MainDimmer = parts[1] == ON;
                    break;
                case MAIN_POWER_COMMAND:
                    _model.MainPower = parts[1] == ON;
                    break;
                case MAIN_DOLBY_DRC_COMMAND:
                    _model.MainDolbyDRC = parts[1];
                    break;
                case string source when source.StartsWith(SOURCE_PREFIX_COMMAND):
                    ParseSourceName(parts);
                    SetModelMainSourceName();
                    break;
                default:
                    break;
            }
        }
    }

    private async Task WriteCommandAync(string command)
    {
        await EnsureConnectedAsync(ConnectTimeout);
        try
        {
            await _client!.WriteAsync(command);
        }
        catch (Exception ex) when (ex is IOException or SocketException or InvalidOperationException or ObjectDisposedException)
        {
            // The connection died since EnsureConnectedAsync (e.g. the NAD went to sleep).
            // Force a fresh connection and retry the command once.
            Debug.WriteLine($"Write failed, reconnecting and retrying: {ex}");
            lock (_lock)
            {
                DisposeLocked();
            }
            await EnsureConnectedAsync(ConnectTimeout);
            await _client!.WriteAsync(command);
        }
    }

    private async Task WriteSetCommandAsync(string name, string value) => await WriteCommandAync($"{name}={value}{COMMAND_END}");

    private async Task WritePlusCommandAsync(string speaker) => await WriteCommandAync($"{speaker}+{COMMAND_END}");

    private async Task WriteMinusCommandAsync(string speaker) => await WriteCommandAync($"{speaker}-{COMMAND_END}");

    private async Task WriteQueryCommandAsync(string name) => await WriteCommandAync($"{name}?{COMMAND_END}");

    private void SetModelMainSourceName()
    {
        if (int.TryParse(_model?.MainSource, out var id))
        {
            var name = _sources[id - 1];
            if (name != null)
            {
                Debug.WriteLine($"Setting MainSourceName to '{name}'");
                _model.MainSourceName = name;
            }
        }
    }

    private void ParseSourceName(string[] parts)
    {
        Debug.WriteLine($"Parsing: {parts[0]}");
        var number = parts[0].Replace(SOURCE_PREFIX_COMMAND, "");
        number = number.Replace(".Name", "");
        if (int.TryParse(number, out var result))
        {
            if (result is > -1 and < 11)
            {
                _sources[result - 1] = parts[1];
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;

        lock (_lock)
        {
            DisposeLocked();
        }

        // Note: _connectGate is deliberately not disposed — a waiter may still be inside
        // WaitAsync and disposing it underneath them would throw ObjectDisposedException.
        // It is tiny and is garbage-collected with the NadRemote.
    }

    /// <summary>Must be called while holding <see cref="_lock"/>.</summary>
    private void DisposeLocked()
    {
        // Cancel (not just dispose) so in-flight connect/read tokens fire their cancellation
        // handlers; a disposed CTS would leave linked tokens in a state that skips
        // cancellation callbacks. The CTS object is replaced on the next connect.
        _tokenSource?.Cancel();
        _tokenSource?.Dispose();
        _tokenSource = null;
        _client?.Dispose();
        _client = null;
    }
}
