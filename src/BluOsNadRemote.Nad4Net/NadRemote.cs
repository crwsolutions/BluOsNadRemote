﻿using Nad4Net.Extensions;
using Nad4Net.Model;
using PrimS.Telnet;
using System;
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

    public async Task GetCommandListAsync(Action<CommandList> resultHandler)
    {
        await CheckConnection();
        await WriteQueryAsync(MAIN_MODEL_COMMAND);
        await WriteQueryAsync(MAIN_SOURCE_COMMAND);
        await WriteQueryAsync(MAIN_AUDIO_CODEC_COMMAND);
        await WriteQueryAsync(MAIN_AUDIO_CHANNELS_COMMAND);
        await WriteQueryAsync(MAIN_AUDIO_RATE_COMMAND);
        await WriteQueryAsync(MAIN_VIDEO_ARC_COMMAND);
        await WriteQueryAsync(MAIN_LISTENINGMODE_COMMAND);
        await WriteQueryAsync(DIRAC1_STATE_COMMAND);
        await WriteQueryAsync(DIRAC1_NAME_COMMAND);
        await WriteQueryAsync(DIRAC2_STATE_COMMAND);
        await WriteQueryAsync(DIRAC2_NAME_COMMAND);
        await WriteQueryAsync(DIRAC3_STATE_COMMAND);
        await WriteQueryAsync(DIRAC3_NAME_COMMAND);
        await WriteQueryAsync(MAIN_DIRAC_COMMAND);
        await WriteQueryAsync(MAIN_TRIM_SUB_COMMAND);
        await WriteQueryAsync(MAIN_TRIM_SURROUND_COMMAND);
        await WriteQueryAsync(MAIN_TRIM_CENTER_COMMAND);
        await WriteQueryAsync(MAIN_DIMMER_COMMAND);
        await WriteQueryAsync(MAIN_POWER_COMMAND);
        await WriteQueryAsync(MAIN_DOLBY_DRC_COMMAND);
        for (var i = 0; i < _sources.Length; i++)
        {
            await WriteQueryAsync($"{SOURCE_PREFIX_COMMAND}{i + 1}.Name");
        }
        Parse(await _client.ReadAsync());
        resultHandler.Invoke(_model);
    }

    public async Task DoSurroundPlus() => await WritePlusCommand(MAIN_TRIM_SURROUND_COMMAND);
    public async Task DoSurroundMinus() => await WriteMinusCommand(MAIN_TRIM_SURROUND_COMMAND);
    public async Task DoSubPlus() => await WritePlusCommand(MAIN_TRIM_SUB_COMMAND);
    public async Task DoSubMinus() => await WriteMinusCommand(MAIN_TRIM_SUB_COMMAND);
    public async Task DoCenterPlus() => await WritePlusCommand(MAIN_TRIM_CENTER_COMMAND);
    public async Task DoCenterMinus() => await WriteMinusCommand(MAIN_TRIM_CENTER_COMMAND);

    public async Task ToggleOnOffAsync()
    {
        if (_model.MainPower == false)
        {
            await WriteCommand(MAIN_POWER_COMMAND, ON);
        }
        else
        {
            await WriteCommand(MAIN_POWER_COMMAND, OFF);
        }
    }

    public async Task ToggleDimmerAsync()
    {
        if (_model.MainDimmer == false)
        {
            await WriteCommand(MAIN_DIMMER_COMMAND, ON);
        }
        else
        {
            await WriteCommand(MAIN_DIMMER_COMMAND, OFF);
        }
    }

    public async Task SetListeningModeAsync(string value)
    {
        await WriteCommand(MAIN_LISTENINGMODE_COMMAND, value);
    }

    public async Task SetMainDiracAsync(int value)
    {
        await WriteCommand(MAIN_DIRAC_COMMAND, (value + 1).ToString());
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
        var numLines = s == string.Empty ? 0 : s.Count(c => c.Equals(COMMAND_END)) + 1;
        Debug.WriteLine($"{numLines} properties received");
#endif

        string line;
        while ((line = strReader.ReadLine()) != null)
        {
            Debug.WriteLine(line);
            var parts = line.Split(_equalsSeparator);
            switch (parts[0])
            {
                case MAIN_MODEL_COMMAND:
                    _model.MainModel = parts[1];
                    break;
                case MAIN_SOURCE_COMMAND:
                    _model.MainSource = parts[1];
                    SetMainSourceName();
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
                    _model.MainDirac = int.Parse(parts[1]) - 1;
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
                    _model.MainTrimSub = int.Parse(parts[1]);
                    break;
                case MAIN_TRIM_SURROUND_COMMAND:
                    _model.MainTrimSurround = int.Parse(parts[1]);
                    break;
                case MAIN_TRIM_CENTER_COMMAND:
                    _model.MainTrimCenter = int.Parse(parts[1]);
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
                    SetMainSourceName();
                    break;
                default:
                    break;
            }
        }
    }

    private async Task WriteCommand(string command, string value)
    {
        await CheckConnection();
        await _client.WriteLineAsync($"{command}={value}");
    }

    private async Task WritePlusCommand(string speaker)
    {
        await CheckConnection();
        await _client.WriteAsync($"{speaker}+{COMMAND_END}");
    }

    private async Task WriteMinusCommand(string speaker)
    {
        await CheckConnection();
        await _client.WriteAsync($"{speaker}-{COMMAND_END}");
    }

    private async Task WriteQueryAsync(string command) => await _client.WriteAsync($"{command}?{COMMAND_END}");

    private void SetMainSourceName()
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
        _tokenSource?.Cancel();
        _client?.Dispose();
        _client = null;
    }
}
