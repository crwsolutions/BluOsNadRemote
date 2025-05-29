namespace BluOsNadRemote.App.Repositories;

public sealed partial class CultureOverrideRepository
{
    private const string CULTURE_ID = "culture_override";

    [Dependency]
    private readonly IPreferences _preferences;

    internal string GetCultureOverride() => _preferences.Get(CULTURE_ID, "");

    internal void StoreCultureOverride(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            _preferences.Remove(CULTURE_ID);
        }
        else
        {
            _preferences.Set(CULTURE_ID, name);
        }
    }
}
