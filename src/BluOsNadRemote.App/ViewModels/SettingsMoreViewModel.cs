using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public sealed partial class SettingsMoreViewModel : BaseViewModel
{
    [Dependency]
    private readonly LanguageService _languageService;

    partial void PostConstruct()
    {
        switch (_languageService.GetCultureOverride())
        {
            case Language.EN_US:
                EnIsChecked = true;
                break;
            case Language.NL_NL:
                NlIsChecked = true;
                break;
            default:
                DefaultIsChecked = true;
                break;
        }
    }

    public string EN_US { get; } = Language.EN_US;
    public string NL_NL { get; } = Language.NL_NL;

    internal void SetCulture(string value) => _languageService.SetCulture(value);

    [ObservableProperty]
    private bool _defaultIsChecked;

    [ObservableProperty]
    private bool _enIsChecked;

    [ObservableProperty]
    private bool _nlIsChecked;
}
