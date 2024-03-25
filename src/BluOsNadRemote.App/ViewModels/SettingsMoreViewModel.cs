using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public sealed partial class SettingsMoreViewModel : BaseViewModel
{
    [Dependency]
    private readonly LanguageService _languageService;

    [Dependency]
    private readonly ThemeService _themeService;

    partial void PostConstruct()
    {
        SetLanguageChecks();
        SetThemeChecks();
    }

    private void SetLanguageChecks()
    {
        switch (_languageService.CurrentLanguageOverride)
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
    private bool _defaultLanguageIsChecked;

    [ObservableProperty]
    private bool _enIsChecked;

    [ObservableProperty]
    private bool _nlIsChecked;

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
    private bool _defaultThemeIsChecked;

    [ObservableProperty]
    private bool _darkIsChecked;

    [ObservableProperty]
    private bool _lightIsChecked;
}
