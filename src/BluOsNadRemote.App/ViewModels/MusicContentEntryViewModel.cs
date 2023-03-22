using Blu4Net;

namespace BluOsNadRemote.App.ViewModels;

public partial class MusicContentEntryViewModel
{
    [Dependency]
    private readonly MusicContentEntry _musicContentEntry;

    public bool IsPlayableAndResolvable => _musicContentEntry.PlayURL != null && _musicContentEntry.IsResolvable == true;

    public MusicContentEntry Entry => _musicContentEntry;

    public bool HasImage => Entry.ImageUri != null;

    public bool HasContextMenu => Entry.HasContextMenu;
}
