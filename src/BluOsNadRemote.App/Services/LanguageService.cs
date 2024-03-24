using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Resources.Languages;
using System.Globalization;

namespace BluOsNadRemote.App.Services;

public sealed partial class LanguageService
{
    [Dependency]
    private readonly CultureOverrideRepository _cultureRepository;

    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    private readonly CultureInfo _deviceCulture = CultureInfo.CurrentCulture;

    public void Initialize()
    {
        var cultureOverride = _cultureRepository.GetCultureOverride();
        SetCulture(cultureOverride);
    }

     public string GetCultureOverride() => _cultureRepository.GetCultureOverride();

    public void SetCulture(string value)
    {
        var cultureInfo = string.IsNullOrEmpty(value) ? _deviceCulture : new CultureInfo(value);
        Language.Instance.SetCulture(cultureInfo);
        _bluPlayerService.CultureInfo = cultureInfo;

        if (string.IsNullOrEmpty(value))
        {
            _cultureRepository.ClearCultureOverride();
        }
        else 
        {
            _cultureRepository.SetCultureOverride(value);
        }
    }
}
