namespace BluOsNadRemote.App.Models;
public class BluPlayerDiscoverResult
{
    public BluPlayerDiscoverResult(string message, bool hasDiscovered)
    {
        Message = message;
        HasDiscovered = hasDiscovered;
    }

    public string Message { get; }

    public bool HasDiscovered { get; }
}

