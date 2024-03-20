namespace BluOsNadRemote.App.Repositories;

public sealed partial class CultureOverrideRepository
{
    private const string CULTURE_ID = "culture_override";

    [Dependency]
    private readonly IPreferences _preferences;

    internal string GetCultureOverride() => _preferences.Get<string>(CULTURE_ID, null);

    internal void SetCultureOverride(string culture) => _preferences.Set(CULTURE_ID, culture);

    internal void ClearCultureOverride() => _preferences.Remove(CULTURE_ID);
}
