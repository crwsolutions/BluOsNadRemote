using Blu4Net;

namespace BluOsNadRemote.App.ViewModels;

[ObservableObject]
public partial class MusicContentCategoryViewModel : List<MusicContentEntryViewModel>
{
    public MusicContentCategoryViewModel(MusicContentCategory category)
    {
        AddRange(category.Entries.Select(e => new MusicContentEntryViewModel(e)));
        Name = $"{category.Name} ({Count} items)";
    }

    [ObservableProperty]
    private string _name;
}
