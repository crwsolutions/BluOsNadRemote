namespace BluOsNadRemote.App.Models;

[DebuggerDisplay("[{Number}] {Name}")]
public sealed class Favorite
{
    public int Number { get; }
    public string Name { get; }

    private string? _imageUri;
    public string? ImageUri
    {
        get
        {
            return _imageUri;
        }

        set
        {
            _imageUri = !string.IsNullOrEmpty(value) ? value : "fallback.png";
        }
    }

    public Favorite(Blu4Net.PlayerPreset preset)
    {
        Number = preset.Number;
        Name = preset.Name;
        ImageUri = preset.ImageUri?.ToString();
    }
}
