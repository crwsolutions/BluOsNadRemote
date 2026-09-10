using BluOsNadRemote.App.Repositories;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public sealed partial class SettingsMoreViewModel : BaseViewModel
{
    [Dependency]
    private readonly LanguageService _languageService;

    [Dependency]
    private readonly ThemeService _themeService;

    [Dependency]
    private readonly VolumeDisplayRepository _volumeDisplayRepository;

    partial void PostConstruct()
    {
        SetLanguageChecks();
        SetThemeChecks();
        ShowDecibel = _volumeDisplayRepository.GetShowDecibel();
    }

    [ObservableProperty]
    public partial bool ShowDecibel { get; set; }

    partial void OnShowDecibelChanged(bool value) => _volumeDisplayRepository.SetShowDecibel(value);

    private void SetLanguageChecks()
    {
        switch (_languageService.LanguageOverrideName)
        {
            case LanguageService.EN_US:
                EnIsChecked = true;
                break;
            case LanguageService.NL_NL:
                NlIsChecked = true;
                break;
            default:
                DefaultLanguageIsChecked = true;
                break;
        }
    }

    public string EN_US { get; } = LanguageService.EN_US;
    public string NL_NL { get; } = LanguageService.NL_NL;

    internal void SetCulture(string value) => _languageService.SetCulture(value);

    [ObservableProperty]
    public partial bool DefaultLanguageIsChecked { get; set; }

    [ObservableProperty]
    public partial bool EnIsChecked { get; set; }

    [ObservableProperty]
    public partial bool NlIsChecked { get; set; }

    private void SetThemeChecks()
    {
        switch (_themeService.GetThemeOverride())
        {
            case ThemeService.DARK:
                DarkIsChecked = true;
                break;
            case ThemeService.LIGHT:
                LightIsChecked = true;
                break;
            default:
                DefaultThemeIsChecked = true;
                break;
        }
    }

    public string DARK { get; } = ThemeService.DARK;
    public string LIGHT { get; } = ThemeService.LIGHT;

    internal void SetTheme(string value) => _themeService.SetTheme(value);

    [ObservableProperty]
    public partial bool DefaultThemeIsChecked { get; set; }

    [ObservableProperty]
    public partial bool DarkIsChecked { get; set; }

    [ObservableProperty]
    public partial bool LightIsChecked { get; set; }
}
