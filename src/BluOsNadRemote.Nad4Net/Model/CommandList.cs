
namespace Nad4Net.Model;

public class CommandList
{
    public string? MainModel { get; set; }
    public string? MainAudioCODEC { get; set; }
    public string? MainSource { get; set; }
    public string? MainSourceName { get; set; }
    public string? MainAudioChannels { get; set; }
    public string? MainAudioRate { get; set; }
    public string? MainVideoARC { get; set; }
    public string? MainListeningMode { get; set; }
    public string? Dirac1State { get; set; }
    public string? Dirac1Name { get; set; }
    public string? Dirac2State { get; set; }
    public string? Dirac2Name { get; set; }
    public string? Dirac3State { get; set; }
    public string? Dirac3Name { get; set; }
    public int MainDirac { get; set; }
    public int MainTrimSub { get; set; }
    public int MainTrimSurround { get; set; }
    public int MainTrimCenter { get; set; }
    public bool MainDimmer { get; set; }
    public bool MainPower { get; set; }
    public string? MainDolbyDRC { get; set; }
}
