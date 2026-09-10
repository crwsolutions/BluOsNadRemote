namespace BluOsNadRemote.App.Repositories;

public sealed partial class VolumeDisplayRepository
{
    private const string VOLUME_DISPLAY_ID = "volume_display_decibel";

    [Dependency]
    private readonly IPreferences _preferences;

    internal bool GetShowDecibel() => _preferences.Get(VOLUME_DISPLAY_ID, false);

    internal void SetShowDecibel(bool showDecibel) => _preferences.Set(VOLUME_DISPLAY_ID, showDecibel);
}
