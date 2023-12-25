namespace BluOsNadRemote.App.Models;
public class BluPlayerConnectResult
{
    internal BluPlayerConnectResult(string message, bool isConnected)
    {
        Message = message;
        IsConnected = isConnected;
    }

    public string Message { get; }

    public bool IsConnected { get; }
}

