using Nad4Net.Extensions;
using Nad4Net.Model;
using PrimS.Telnet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nad4Net;

public class NadRemote : IDisposable
{
    private CancellationTokenSource _tokenSource;
    private readonly string[] _sources = new string[10];
    private Client _client;
    private readonly string _host;
    private const int Port = 23;
    private TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);
    private TimeSpan RetryDelay { get; } = TimeSpan.FromSeconds(5);
    public IObservable<CommandList> CommandChanges { get; }

    private readonly CommandList _model = new();
    public Uri Endpoint { get; }

    private static readonly char[] _equalsSeparator = ['='];

    public async Task<string> PingAsync()
    {
        await ReConnectAsync();
        if (_client.IsConnected)
        {
            return await _client.ReadAsync();
        }
        return null;
    }

    public NadRemote(Uri endpoint)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _host = endpoint.Host;

        CommandChanges = SetupChangeDetectionLoop().Retry(RetryDelay).Publish().RefCount();
    }

    private async Task WriteCommand(string command, string value)
    {
        await CheckConnection();
        await _client.WriteLineAsync($"{command}={value}");
    }

    private async Task WritePlusCommand(string speaker)
    {
        await CheckConnection();
        await _client.WriteLineAsync($"Main.Trim.{speaker}+");
    }

    public async Task GetCommandListAsync(Action<CommandList> resultHandler)
    {
        await CheckConnection();
        await _client.WriteLineAsync("Main.Model?");
        await _client.WriteLineAsync("Main.Source?");
        await _client.WriteLineAsync("Main.Audio.CODEC?");
        await _client.WriteLineAsync("Main.Audio.Channels?");
        await _client.WriteLineAsync("Main.Audio.Rate?");
        await _client.WriteLineAsync("Main.Video.ARC?");
        await _client.WriteLineAsync("Main.ListeningMode?");
        await _client.WriteLineAsync("Dirac1.State?");
        await _client.WriteLineAsync("Dirac1.Name?");
        await _client.WriteLineAsync("Dirac2.State?");
        await _client.WriteLineAsync("Dirac2.Name?");
        await _client.WriteLineAsync("Dirac3.State?");
        await _client.WriteLineAsync("Dirac3.Name?");
        await _client.WriteLineAsync("Main.Dirac?");
        await _client.WriteLineAsync("Main.Trim.Sub?");
        await _client.WriteLineAsync("Main.Trim.Surround?");
        await _client.WriteLineAsync("Main.Trim.Center?");
        await _client.WriteLineAsync("Main.Dimmer?");
        await _client.WriteLineAsync("Main.Power?");
        await _client.WriteLineAsync("Main.Dolby.DRC?");
        for (int i = 0; i < _sources.Length; i++) 
        {
            await _client.WriteLineAsync($"Source{i + 1}.Name?");
        }
        Parse(await _client.ReadAsync());
        resultHandler.Invoke(_model);
    }

    private async Task WriteMinusCommand(string speaker)
    {
        await CheckConnection();
        await _client.WriteLineAsync($"Main.Trim.{speaker}-");
    }

    public async Task DoSurroundPlus() => await WritePlusCommand("Surround");
    public async Task DoSurroundMinus() => await WriteMinusCommand("Surround");
    public async Task DoSubPlus() => await WritePlusCommand("Sub");
    public async Task DoSubMinus() => await WriteMinusCommand("Sub");
    public async Task DoCenterPlus() => await WritePlusCommand("Center");
    public async Task DoCenterMinus() => await WriteMinusCommand("Center");

    public async Task ToggleOnOffAsync()
    {
        if (_model.MainPower == false)
        {
            await WriteCommand("Main.Power", "On");
        }
        else
        {
            await WriteCommand("Main.Power", "Off");
        }
    }

    public async Task ToggleDimmerAsync()
    {
        if (_model.MainDimmer == false)
        {
            await WriteCommand("Main.Dimmer", "On");
        }
        else
        {
            await WriteCommand("Main.Dimmer", "Off");
        }
    }

    public async Task SetListeningModeAsync(string value)
    {
        await WriteCommand("Main.ListeningMode", value);
    }

    public async Task SetMainDiracAsync(int value)
    {
        await WriteCommand("Main.Dirac", (value + 1).ToString());
    }

    private async Task ReConnectAsync()
    {
        Debug.WriteLine("(Re-)connecting the telnet connection");

        Dispose();
        _tokenSource = new CancellationTokenSource();
        _client = new Client(_host, Port, _tokenSource.Token);
        Parse(await _client.ReadAsync());
    }
    private IObservable<CommandList> SetupChangeDetectionLoop()
    {
        return Observable.Create<CommandList>((observer, cancellationToken) =>
        {
            return Task.Run(async () =>
            {
                Debug.WriteLine("Starting new telnet listener");
                await CheckConnection();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_client == null)
                        {
                            await ReConnectAsync();
                        }
                        var s = await _client.ReadAsync(Timeout);
                        Parse(s);
                        observer.OnNext(_model);
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

                Debug.WriteLine($"Stopped the telnet listener, toke: {cancellationToken.IsCancellationRequested}");

            }, cancellationToken);
        });
    }

    private async Task CheckConnection()
    {
        if (_client == null || !_client.IsConnected)
        {
            await ReConnectAsync();
        }
    }

    private void Parse(string s)
    {
        var strReader = new StringReader(s);
#if DEBUG
        var numLines = s == string.Empty ? 0 : s.Count(c => c.Equals('\n')) + 1;
        Debug.WriteLine($"{numLines} properties received");
#endif

        string line;
        while ((line = strReader.ReadLine()) != null)
        {
            Debug.WriteLine(line);
            var parts = line.Split(_equalsSeparator);
            switch (parts[0])
            {
                case "Main.Model":
                    _model.MainModel = parts[1];
                    break;
                case "Main.Source":
                    _model.MainSource = parts[1];
                    break;
                case "Main.Audio.CODEC":
                    _model.MainAudioCODEC = parts[1];
                    break;
                case "Main.Audio.Channels":
                    _model.MainAudioChannels = parts[1];
                    break;
                case "Main.Audio.Rate":
                    _model.MainAudioRate = parts[1];
                    break;
                case "Main.Video.ARC":
                    _model.MainVideoARC = parts[1];
                    break;
                case "Main.ListeningMode":
                    _model.MainListeningMode = parts[1];
                    break;
                case "Main.Dirac":
                    _model.MainDirac = int.Parse(parts[1]) - 1;
                    break;
                case "Dirac1.State":
                    _model.Dirac1State = parts[1];
                    break;
                case "Dirac1.Name":
                    _model.Dirac1Name = parts[1];
                    break;
                case "Dirac2.State":
                    _model.Dirac2State = parts[1];
                    break;
                case "Dirac2.Name":
                    _model.Dirac2Name = parts[1];
                    break;
                case "Dirac3.State":
                    _model.Dirac3State = parts[1];
                    break;
                case "Dirac3.Name":
                    _model.Dirac3Name = parts[1];
                    break;
                case "Main.Trim.Sub":
                    _model.MainTrimSub = int.Parse(parts[1]);
                    break;
                case "Main.Trim.Surround":
                    _model.MainTrimSurround = int.Parse(parts[1]);
                    break;
                case "Main.Trim.Center":
                    _model.MainTrimCenter = int.Parse(parts[1]);
                    break;
                case "Main.Dimmer":
                    _model.MainDimmer = parts[1] == "On";
                    break;
                case "Main.Power":
                    _model.MainPower = parts[1] == "On";
                    break;
                case "Main.Dolby.DRC":
                    _model.MainDolbyDRC = parts[1];
                    break;
                default:
                    if (parts[0] == $"Source{_model.MainSource}.Name")
                    {
                        _model.MainSourceName = parts[1];
                    }
                    ParseSourceName(parts);
                    break;
            }
        }
    }

    private void ParseSourceName(string[] parts)
    {
        Debug.WriteLine($"Parsing: {parts[0]}");
        var number = parts[0].Replace("Source", "");
        number = number.Replace(".Name", "");
        if (int.TryParse(number, out var result))
        {
            if (result is > -1 and < 11)
            {
                _sources[result-1] = parts[1];
            }
        }
    }

    public void Dispose()
    {
        _tokenSource?.Cancel();
        _client?.Dispose();
        _client = null;
    }
}
