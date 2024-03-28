﻿using BluOsNadRemote.App.Repositories;
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

    public void Initialize() => DetermineCutureAndNotifySubscribers(CurrentLanguageOverride);

    public IObservable<CultureInfo> LanguageObservable() => _languageSubject.AsObservable();

    public void SetCulture(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            _cultureRepository.ClearCultureOverride();
        }
        else
        {
            _cultureRepository.SetCultureOverride(name);
        }

        DetermineCutureAndNotifySubscribers(name);
    }

    internal CultureInfo CurrentLanguage => _languageSubject.Value;

    internal string CurrentLanguageOverride => _cultureRepository.GetCultureOverride();

    private void DetermineCutureAndNotifySubscribers(string name)
    {
        var cultureInfo = string.IsNullOrEmpty(name) ? _deviceCulture : new CultureInfo(name);
        _languageSubject.OnNext(cultureInfo);
    }
}
