using BluOsNadRemote.App.Repositories;

namespace BluOsNadRemote.App.Services;

public sealed partial class ThemeService
{
    public const string DARK = "dark";

    public const string LIGHT = "light";

    [Dependency]
    private readonly ThemeOverrideRepository _themeRepository;

    public void Initialize()
    {
        Application.Current!.UserAppTheme = _themeRepository.GetThemeOverride() switch
        {
            DARK => AppTheme.Dark,
            LIGHT => AppTheme.Light,
            _ => AppInfo.Current.RequestedTheme,
        };
    }

    public string? GetThemeOverride() => _themeRepository.GetThemeOverride();

    public void SetTheme(string value)
    {
        switch (value)
        {
            case DARK:
                SetDarkTheme();
                break;
            case LIGHT:
                SetLightTheme();
                break;
            default:
                ClearTheme();
                break;
        }
    }

    private void SetDarkTheme()
    {
        Application.Current!.UserAppTheme = AppTheme.Dark;
        _themeRepository.SetThemeOverride(DARK);
    }

    private void SetLightTheme()
    {
        Application.Current!.UserAppTheme = AppTheme.Light;
        _themeRepository.SetThemeOverride(LIGHT);
    }

    private void ClearTheme()
    {
        Application.Current!.UserAppTheme = AppInfo.Current.RequestedTheme;
        _themeRepository.ClearThemeOverride();
    }
}
