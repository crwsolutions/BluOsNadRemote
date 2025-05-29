using Blu4Net;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

[DebuggerDisplay("MusicContentEntryViewModel: '{Entry.Name}'")]
public partial class MusicContentEntryViewModel
{
    [Dependency]
    private readonly MusicContentEntry _musicContentEntry;

    [Dependency]
    private readonly BluPlayerService _bluPlayerService;

    public bool IsPlayableAndResolvable => _musicContentEntry.PlayURL != null && _musicContentEntry.IsResolvable == true;

    public MusicContentEntry Entry => _musicContentEntry;

    public bool HasImage => Entry.ImageUri != null;

    public bool HasContextMenu => Entry.HasContextMenu;

    [RelayCommand]
    private async Task DisplayActionSheetAsync()
    {
        Debug.WriteLine(Entry.Name);

        var contextMenu = await Entry.ResolveContextMenu();

        Debug.WriteLine(contextMenu.Entries.Count);
        var options = contextMenu.Entries.Select(t => t.Name).ToArray();

        var page = Shell.Current.CurrentPage;// as BrowsePage;

        var action = await page.DisplayActionSheet(AppResources.Actions, AppResources.Cancel, null, options);

        var actionEntry = contextMenu.Entries.FirstOrDefault(e => e.Name == action);

        Debug.WriteLine("Action clicked: " + actionEntry?.ActionURL);

        if (actionEntry == null)
        {
            return;
        }

        await _bluPlayerService.BluPlayer!.MusicBrowser.PlayURL(actionEntry.ActionURL);
    }
}
