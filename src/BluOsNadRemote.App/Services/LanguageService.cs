using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Resources.Languages;
using System.Globalization;

namespace BluOsNadRemote.App.Services;

public sealed partial class LanguageService
{
    [Dependency]
    private readonly CultureOverrideRepository _cultureRepository;

    private readonly CultureInfo _deviceCulture = CultureInfo.CurrentCulture;

    public void Initialize()
    {
        var cultureOverride = _cultureRepository.GetCultureOverride();
        if (cultureOverride is not null)
        {
            Language.Instance.SetCulture(new CultureInfo(cultureOverride));
        }
    }

    public string GetCultureOverride() => _cultureRepository.GetCultureOverride();

    public void SetCulture(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Language.Instance.SetCulture(_deviceCulture);
            _cultureRepository.ClearCultureOverride();
            return;
        }

        Language.Instance.SetCulture(new CultureInfo(value));
        _cultureRepository.SetCultureOverride(value);
    }
}
