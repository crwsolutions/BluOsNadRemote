using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Resources.Languages;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BluOsNadRemote.App.Services;

public sealed partial class LanguageService
{
    private readonly CultureInfo _deviceCulture = CultureInfo.CurrentCulture;

    private readonly BehaviorSubject<CultureInfo> _languageSubject = new(CultureInfo.CurrentCulture);

    [Dependency]
    private readonly CultureOverrideRepository _cultureRepository;

    // supported languages
    public const string EN_US = "en-US";
    public const string NL_NL = "nl-NL";

    public void Initialize() => SetCurrentCulture(LanguageOverrideName);

    public IObservable<CultureInfo> LanguageObservable() => _languageSubject.AsObservable();

    public void SetCulture(string name)
    {
        _cultureRepository.StoreCultureOverride(name);
        SetCultureAndNotifySubscribers(name);
    }

    internal string LanguageOverrideName => _cultureRepository.GetCultureOverride();

    private void SetCultureAndNotifySubscribers(string name)
    {
        SetCurrentCulture(name);
        _languageSubject.OnNext(AppResources.Culture);
    }

    private void SetCurrentCulture(string name) 
        => AppResources.Culture = string.IsNullOrEmpty(name) ? _deviceCulture : new CultureInfo(name);
}
