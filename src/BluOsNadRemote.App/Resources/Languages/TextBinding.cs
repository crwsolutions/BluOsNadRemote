using System.ComponentModel;
using System.Globalization;

namespace BluOsNadRemote.App.Resources.Languages;

public static class TextBinding
{
    public static TextViewModel Source { get; } = new();

    public sealed class TextViewModel : INotifyPropertyChanged
    {
        public string this[string resourceKey]
        => AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture) ?? string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnNext(CultureInfo value)
        {
            AppResources.Culture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); //null = Notify all
        }
    }
}
