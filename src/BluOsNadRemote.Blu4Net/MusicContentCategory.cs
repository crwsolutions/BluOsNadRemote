using BluOsNadRemote.Blu4Net.Channel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BluOsNadRemote.Blu4Net;

public class MusicContentCategory
{
    public MusicContentCategory(BluChannel channel, MusicContentNode parent, BrowseContentResponse.Category response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Name = response.Text;
        NextKey = string.IsNullOrEmpty(response.NextKey) ? null : response.NextKey;
        Entries = response.Items != null ? response.Items.Select(element => new MusicContentEntry(channel, parent, element)).ToArray() : new MusicContentEntry[0];
    }

    public IReadOnlyCollection<MusicContentEntry> Entries { get; }

    public string Name { get; }

    public string NextKey { get; }
}