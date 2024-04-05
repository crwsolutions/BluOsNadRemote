using BluOsNadRemote.App.Services;
using System.ComponentModel;
using System.Globalization;

namespace BluOsNadRemote.App.Resources.Languages;

public class TextsViewModel : INotifyPropertyChanged
{
    public static TextsViewModel Instance { get; } = new();

    private TextsViewModel()
        => ((App)Application.Current)
            .ServiceProvider.GetService<LanguageService>()
            .LanguageObservable()
            .Subscribe(NotifyPropertyChanged);

    public string this[string resourceKey]
        => AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture) ?? string.Empty;

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(CultureInfo value)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); //null = Notify all
}
