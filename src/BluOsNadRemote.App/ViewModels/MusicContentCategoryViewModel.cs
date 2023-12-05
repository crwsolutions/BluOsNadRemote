﻿using Blu4Net;
using BluOsNadRemote.App.Services;

namespace BluOsNadRemote.App.ViewModels;

public partial class MusicContentCategoryViewModel : ObservableCollection<MusicContentEntryViewModel>
{
    public MusicContentCategoryViewModel(MusicContentCategory category, BluPlayerService bluPlayerService)
    {
        AddRange(category.Entries.Select(e => new MusicContentEntryViewModel(e, bluPlayerService)));
        Name = $"{category.Name} ({Count} items)";
    }

    public void AddRange(IEnumerable<MusicContentEntryViewModel> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public MusicContentCategoryViewModel(bool hasNext, IReadOnlyCollection<MusicContentEntry> entries, BluPlayerService bluPlayerService)
    {
        AddRange(entries.Select(e => new MusicContentEntryViewModel(e, bluPlayerService)));
        Name = hasNext ? $"More than {Count} items..." : $"{Count} items";
    }

    public string Name { get; set; }
}
