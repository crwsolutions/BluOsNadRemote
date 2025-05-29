namespace BluOsNadRemote.App.Repositories;

public sealed partial class ThemeOverrideRepository
{
    private const string THEME_ID = "theme_override";

    [Dependency]
    private readonly IPreferences _preferences;

    internal string GetThemeOverride() => _preferences.Get(THEME_ID, "");

    internal void SetThemeOverride(string theme) => _preferences.Set(THEME_ID, theme);

    internal void ClearThemeOverride() => _preferences.Remove(THEME_ID);
}
