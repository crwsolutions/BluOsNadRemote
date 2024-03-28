using BluOsNadRemote.App.Services;
using System.ComponentModel;
using System.Globalization;

namespace BluOsNadRemote.App.Resources.Languages;

public static class TextBinding
{
    public static TextViewModel Source { get; } = new();

    public sealed class TextViewModel : INotifyPropertyChanged
    {
        public TextViewModel()
        {
            SubscribeToLanguageChanges();
        }
        private void SubscribeToLanguageChanges()
        {
            var app = Application.Current as App;
            var languageService = app.ServiceProvider.GetService<LanguageService>();
            languageService.LanguageObservable().Subscribe(SetCultureAndNotifyPropertyChanged);
        }

        public string this[string resourceKey]
        => AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture) ?? string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetCultureAndNotifyPropertyChanged(CultureInfo value)
        {
            AppResources.Culture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); //null = Notify all
        }
    }
}
