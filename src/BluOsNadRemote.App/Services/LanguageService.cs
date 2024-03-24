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

    public void Initialize() => PublishLanguage(CurrentLanguageOverride);

    public IObservable<CultureInfo> LanguageObservable() => _languageSubject.AsObservable();

    public void SetCulture(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _cultureRepository.ClearCultureOverride();
        }
        else
        {
            _cultureRepository.SetCultureOverride(value);
        }

        PublishLanguage(value);
    }

    internal CultureInfo CurrentLanguage => _languageSubject.Value;

    internal string CurrentLanguageOverride => _cultureRepository.GetCultureOverride();

    private void PublishLanguage(string value)
    {
        var cultureInfo = string.IsNullOrEmpty(value) ? _deviceCulture : new CultureInfo(value);
        Language.Instance.SetCulture(cultureInfo);
        _languageSubject.OnNext(cultureInfo);
    }
}
