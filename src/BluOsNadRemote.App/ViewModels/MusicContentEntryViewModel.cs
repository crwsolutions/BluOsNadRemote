using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;
using BluOsNadRemote.Blu4Net;

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

        var action = await page.DisplayActionSheetAsync(AppResources.Actions, AppResources.Cancel, null, options);

        var actionEntry = contextMenu.Entries.FirstOrDefault(e => e.Name == action);

        Debug.WriteLine("Action clicked: " + actionEntry?.ActionURL);

        if (actionEntry == null)
        {
            return;
        }

        try
        {
            await _bluPlayerService.BluPlayer!.MusicBrowser.PlayURL(actionEntry.ActionURL);
        }
        catch (Exception exception)
        {
            // The player can reject the action (e.g. "Login to use favourites");
            // show the message instead of crashing.
            Debug.WriteLine(exception);
            await Shell.Current.CurrentPage.DisplayAlertAsync("Alert", AppResources.PlayerActionFailed.Interpolate(exception.Message), "OK");
        }
    }
}
