using System.ComponentModel;
using System.Globalization;

namespace BluOsNadRemote.App.Resources.Languages;

public sealed class Language : INotifyPropertyChanged
{
    private Language()
    {
        AppResources.Culture = CultureInfo.CurrentCulture;
    }

    public static Language Instance { get; } = new();

    public string this[string resourceKey]
        => AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture) ?? string.Empty;

    public event PropertyChangedEventHandler PropertyChanged;

    public void SetCulture(CultureInfo culture)
    {
        AppResources.Culture = culture;
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(null) //Notify all properties
        );
    }
}
