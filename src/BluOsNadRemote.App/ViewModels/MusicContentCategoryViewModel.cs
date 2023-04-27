using Blu4Net;

namespace BluOsNadRemote.App.ViewModels;

public partial class MusicContentCategoryViewModel : ObservableCollection<MusicContentEntryViewModel>
{
    public MusicContentCategoryViewModel(MusicContentCategory category)
    {
        AddRange(category.Entries.Select(e => new MusicContentEntryViewModel(e)));
        Name = $"{category.Name} ({Count} items)";
    }

    public void AddRange(IEnumerable<MusicContentEntryViewModel> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public MusicContentCategoryViewModel(bool hasNext, IReadOnlyCollection<MusicContentEntry> entries)
    {
        AddRange(entries.Select(e => new MusicContentEntryViewModel(e)));
        Name = hasNext ? $"More than {Count} items..." : $"{Count} items";
    }

    public string Name { get; set; }
}
