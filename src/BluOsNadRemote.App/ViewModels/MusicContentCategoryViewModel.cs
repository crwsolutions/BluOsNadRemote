using Blu4Net;
using BluOsNadRemote.App.Extensions;
using BluOsNadRemote.App.Resources.Languages;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

[DebuggerDisplay("MusicContentCategoryViewModel: '{Name}' {Count} Items")]
public sealed class MusicContentCategoryViewModel : List<MusicContentEntryViewModel>
{
    public MusicContentCategoryViewModel(MusicContentCategory category, BluPlayerService bluPlayerService)
    {
        AddRange(category.Entries.Select(e => new MusicContentEntryViewModel(e, bluPlayerService)));
        Name = $"{category.Name} ({Count} items)";
    }

    public MusicContentCategoryViewModel(bool hasNext, IReadOnlyCollection<MusicContentEntry> entries, BluPlayerService bluPlayerService)
    {
        AddRange(entries.Select(e => new MusicContentEntryViewModel(e, bluPlayerService)));
        Name = hasNext ? AppResources.NumMoreItems.Interpolate(Count) : AppResources.NumItems.Interpolate(Count);
    }

    public string Name { get; }

    public override string ToString() => Name;
}
