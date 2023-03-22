using Blu4Net;

namespace BluOsNadRemote.App.ViewModels;

[ObservableObject]
public partial class MusicContentCategoryViewModel : List<MusicContentEntryViewModel>
{
    public MusicContentCategoryViewModel(MusicContentCategory category)
    {
        var entries = category.Entries;
        LoadEntries(entries);
        Name = $"{category.Name} ({Count} items)";
    }

    public MusicContentCategoryViewModel(IReadOnlyCollection<MusicContentEntry> entries)
    {
        LoadEntries(entries);
    }

    private void LoadEntries(IReadOnlyCollection<MusicContentEntry> entries)
    {
        foreach (var entry in entries)
        {
            Add(new MusicContentEntryViewModel(entry));
        }
        Name = $"Returned {Count} items";
    }

    [ObservableProperty]
    private string _name;
}
