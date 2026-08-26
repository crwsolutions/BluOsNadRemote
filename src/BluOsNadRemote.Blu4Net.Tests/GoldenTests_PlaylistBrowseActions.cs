using System;
using BluOsNadRemote.Blu4Net.Channel;
using Xunit;

namespace BluOsNadRemote.Blu4Net.Tests;

public class PlaylistPresetsTests
{
    [Fact]
    public void Read_WithSongs_AllAttributesAndElementsParsed()
    {
        using var reader = Fixture.Reader("PlaylistWithSongs.xml");
        var response = PlaylistResponse.Read(reader);

        Assert.Equal("Calm Piano", response.Name);
        Assert.Equal(160, response.Length);
        Assert.Equal(0, response.Modified);
        Assert.Equal(1, response.Shuffle);
        Assert.Equal(0, response.Repeat);
        Assert.Equal(2, response.Songs.Length);

        var first = response.Songs[0];
        Assert.Equal(25, first.ID);
        Assert.Equal("Deezer:487381362", first.SongID);
        Assert.Equal("61483452", first.AlbumID);
        Assert.Equal("6396188", first.ArtistID);
        Assert.Equal("Deezer", first.Service);
        Assert.Equal("2002", first.Title);
        Assert.Equal("Anne-Marie", first.Artist);
        Assert.Equal("2002", first.Album);
        Assert.Null(first.TrackstationID);
        Assert.Null(first.SimilarstationID);

        // undocumented <fn> child element is skipped, documented extras are read
        var second = response.Songs[1];
        Assert.Equal(26, second.ID);
        Assert.Equal("Tidal:radio:track/139297747", second.TrackstationID);
        Assert.Equal("Tidal:radio:1297273", second.SimilarstationID);
    }

    [Fact]
    public void Read_LengthOnly_NoSongs()
    {
        using var reader = Fixture.Reader("PlaylistLengthOnly.xml");
        var response = PlaylistResponse.Read(reader);

        Assert.Equal(160, response.Length);
        Assert.Empty(response.Songs);
    }

    [Fact]
    public void Read_Empty_NoSongs()
    {
        using var reader = Fixture.Reader("PlaylistEmpty.xml");
        var response = PlaylistResponse.Read(reader);

        Assert.Equal(0, response.Length);
        Assert.Empty(response.Songs);
    }

    [Fact]
    public void Read_Presets_PridAndUndocumentedVolumeAttributeHandled()
    {
        using var reader = Fixture.Reader("Presets.xml");
        var response = PresetsResponse.Read(reader);

        Assert.Equal(3, response.Presets.Length);

        var first = response.Presets[0];
        Assert.Equal(6, first.ID);
        Assert.Equal("Serenity", first.Name);
        Assert.Equal("RadioParadise:/42:4/Serenity", first.Url);
        Assert.Equal("https://img.radioparadise.com/channels/0/042/cover_512x512/0.jpg", first.Image);
        Assert.Equal(-1, first.Volume); // undocumented volume attribute absent → keeps field default

        var second = response.Presets[1];
        Assert.Equal(7, second.ID);
        Assert.Equal("/Load?service=Tidal&id=fd3f797e-a3e9-4de9-a1e2-b5adb6a57cc7", second.Url);
        Assert.Equal(-1, second.Volume);

        var third = response.Presets[2];
        Assert.Equal(8, third.ID);
        Assert.Equal(27, third.Volume); // undocumented volume attribute present → parsed
    }

    [Fact]
    public void Read_Presets_WithAmpersandsAndApostrophe_AllAttributesParsed()
    {
        using var reader = Fixture.CreateReader("""
            <?xml version="1.0" encoding="UTF-8"?>
            <presets prid="0">
              <preset id="1" name="98.9 | NPO Radio 1" url="TuneIn:s17523/http://opml.radiotime.com/Tune.ashx?id=s17523&amp;formats=wma,mp3,aac,ogg,hls&amp;partnerId=8OeGua6y&amp;serial=C0:C1:C0:95:0B:D3" image="https://cdn-radiotime-logos.tunein.com/s17523q.png"></preset>
              <preset id="2" name="92.6 | NPO Radio 2" url="TuneIn:s9483/e6048197/http://opml.radiotime.com/Tune.ashx?id=e6048197&amp;sid=s9483&amp;formats=wma,mp3,aac,ogg,hls&amp;partnerId=8OeGua6y&amp;serial=74:AC:B9:DF:E3:F1" image="https://cdn-radiotime-logos.tunein.com/s9483q.png"></preset>
              <preset id="3" name="94.3 | NPO Klassiek" url="TuneIn:s25548/e344916570/http://opml.radiotime.com/Tune.ashx?id=e344916570&amp;sid=s25548&amp;formats=wma,mp3,aac,ogg,hls&amp;partnerId=8OeGua6y&amp;serial=C0:C1:C0:95:0B:D3" image="https://cdn-profiles.tunein.com/s25548/images/logoq.png?t=638091022660000000"></preset>
              <preset id="4" name="Radio 10 80&#39;s Hits" url="TuneIn:s74982" image="http://cdn-profiles.tunein.com/s74982/images/logog.png?t=638939129170000000"></preset>
            </presets>
            """);

        var response = PresetsResponse.Read(reader);

        Assert.Equal(4, response.Presets.Length);

        var first = response.Presets[0];
        Assert.Equal(1, first.ID);
        Assert.Equal("98.9 | NPO Radio 1", first.Name);
        Assert.Equal("TuneIn:s17523/http://opml.radiotime.com/Tune.ashx?id=s17523&formats=wma,mp3,aac,ogg,hls&partnerId=8OeGua6y&serial=C0:C1:C0:95:0B:D3", first.Url);
        Assert.Equal("https://cdn-radiotime-logos.tunein.com/s17523q.png", first.Image);

        var last = response.Presets[3];
        Assert.Equal(4, last.ID);
        Assert.Equal("Radio 10 80's Hits", last.Name);
        Assert.Equal("TuneIn:s74982", last.Url);
        Assert.Equal("http://cdn-profiles.tunein.com/s74982/images/logog.png?t=638939129170000000", last.Image);
    }

    [Fact]
    public void Read_SetPresetSuccessResponse_PresetsRootParsed()
    {
        // exact response shape seen when the context-menu "Add preset" action succeeds (/SetPreset):
        // the player answers with the updated <presets> list (accepted by the PlayURL dispatch).
        using var reader = Fixture.CreateReader("""
            <?xml version="1.0" encoding="UTF-8"?>
            <presets prid="1">
              <preset id="1" name="98.9 | NPO Radio 1" url="TuneIn:s17523" image="https://cdn-radiotime-logos.tunein.com/s17523q.png"></preset>
              <preset id="5" name="NPO FunX" url="TuneIn:s48069" image="http://cdn-profiles.tunein.com/s48069/images/logog.jpg?t=638608562420000000"></preset>
            </presets>
            """);

        var response = PresetsResponse.Read(reader);

        Assert.Equal(2, response.Presets.Length);
        Assert.Equal("NPO FunX", response.Presets[1].Name);
        Assert.Equal("TuneIn:s48069", response.Presets[1].Url);
    }
}

public class BrowseContentResponseTests
{
    [Fact]
    public void Read_TopLevel_ItemsParsedUndocumentedRootAttributesIgnored()
    {
        using var reader = Fixture.Reader("Browse.xml");
        var response = BrowseContentResponse.Read(reader);

        Assert.Null(response.ServiceName);
        Assert.Null(response.ServiceIcon);
        Assert.Null(response.SearchKey);
        Assert.Null(response.NextKey);
        Assert.Empty(response.Categories);
        Assert.Equal(6, response.Items.Length);

        var playlists = response.Items[0];
        Assert.Equal("playlists", playlists.BrowseKey);
        Assert.Equal("Playlists", playlists.Text);
        Assert.Equal("link", playlists.Type);
        Assert.Equal("/images/ci_myplaylists.png", playlists.Image);
        Assert.Null(playlists.Text2);
        Assert.Null(playlists.PlayURL);
        Assert.Null(playlists.ActionURL);

        // item with playURL + undocumented inputType attribute (attribute ignored)
        var optical = response.Items[2];
        Assert.Equal("Optical Input", optical.Text);
        Assert.Equal("/Play?url=Capture%3Ahw%3A1%2C0%2F1%2F25%2F2%2Finput1", optical.PlayURL);
        Assert.Equal("audio", optical.Type);
        Assert.Null(optical.BrowseKey);
    }

    [Fact]
    public void Read_Level2_ServiceAttributesParsed()
    {
        using var reader = Fixture.Reader("BrowseLevel2.xml");
        var response = BrowseContentResponse.Read(reader);

        Assert.Equal("Deezer", response.ServiceName);
        Assert.Equal("/Sources/images/DeezerIcon.png", response.ServiceIcon);
        Assert.Equal("Deezer:Search", response.SearchKey);
        Assert.Equal(4, response.Items.Length);
        Assert.Equal("/Playlists?service=Deezer&genre=0&category=toplist", response.Items[0].BrowseKey);
    }

    [Fact]
    public void Read_Category_CategoryAndItemsParsed()
    {
        using var reader = Fixture.Reader("BrowseCategory.xml");
        var response = BrowseContentResponse.Read(reader);

        var category = Assert.Single(response.Categories);
        Assert.Equal("Z", category.Text);
        Assert.Equal("Deezer:Albums?start=30", category.NextKey);

        var item = Assert.Single(category.Items);
        Assert.Equal("Essonne History X", item.Text);
        Assert.Equal("Ziak", item.Text2);
        Assert.Equal("album", item.Type);
        Assert.Equal("Deezer:contextMenu/Album?albumid=693798541", item.ContextMenuKey);
        Assert.Equal("/Add?service=Deezer&albumid=693798541&playnow=1", item.PlayURL);

        // a top-level item coexists with categories
        var topLevel = Assert.Single(response.Items);
        Assert.Equal("Popular Albums", topLevel.Text);
    }

    [Fact]
    public void Read_TuneInEmptyItems_EmptyItemKeptWithNullText()
    {
        using var reader = Fixture.Reader("BrowseTuneInEmptyItems.xml");
        var response = BrowseContentResponse.Read(reader);

        Assert.Equal(4, response.Items.Length);
        var empty = response.Items[2];
        Assert.Null(empty.Text);
        Assert.Null(empty.BrowseKey);
        Assert.Equal("Music", response.Items[0].Text);
        Assert.Equal("News", response.Items[3].Text);
    }

    [Fact]
    public void Read_WithContextMenuElement_NestedItemsSkipped()
    {
        const string xml = """
            <browse serviceName="Deezer">
                <item text="Essonne History X" browseKey="Deezer:Album?albumid=1" type="album">
                    <contextMenu>
                        <item text="Favorite" type="favourite-add" actionURL="/AddFavourite?albumid=1"/>
                        <item text="Play now" type="add-now" actionURL="/Add?albumid=1&amp;playnow=1"/>
                    </contextMenu>
                </item>
            </browse>
            """;

        using var reader = Fixture.CreateReader(xml);
        var response = BrowseContentResponse.Read(reader);

        var item = Assert.Single(response.Items);
        Assert.Equal("Essonne History X", item.Text);
        Assert.Equal("Deezer:Album?albumid=1", item.BrowseKey);
        Assert.Null(item.ActionURL); // context-menu child items are not attributes
    }

    [Fact]
    public void Read_ErrorRoot_ReturnsEmptyResponse()
    {
        // deliberate improvement over the old attribute-based deserializer (which returned null):
        // a /Browse error response (root <error>) now yields an object with empty arrays.
        using var reader = Fixture.CreateReader("""
            <error>
                <message>Invalid key</message>
            </error>
            """);

        var response = BrowseContentResponse.Read(reader);

        Assert.Empty(response.Items);
        Assert.Empty(response.Categories);
    }
}

public class AddSlaveAddSongLoadedTests
{
    [Fact]
    public void Read_AddSlave_SlavesParsed()
    {
        using var reader = Fixture.Reader("AddSlave.xml");
        var response = AddSlaveResponse.Read(reader);

        Assert.Equal(2, response.Slave.Length);
        Assert.Equal("192.168.1.153", response.Slave[0].Address);
        Assert.Equal(11000, response.Slave[0].Port);
        Assert.Equal("192.168.1.120", response.Slave[1].Address);
    }

    [Fact]
    public void Read_AddSong()
    {
        using var reader = Fixture.Reader("AddSong.xml");
        var response = AddSongResponse.Read(reader);

        Assert.Equal(26, response.ID);
        Assert.Equal(2, response.Count);
        Assert.Equal(26, response.Length);
        Assert.IsAssignableFrom<LoadedResponse>(response);
    }

    [Fact]
    public void Read_Loaded()
    {
        using var reader = Fixture.Reader("Loaded.xml");
        var response = LoadedResponse.Read(reader) as PlaylistLoadedResponse;

        Assert.NotNull(response);
        Assert.Equal("Deezer", response.Service);
        Assert.Equal(60, response.Entries);
    }

    [Fact]
    public void Read_LoadedResponseDispatch_UnknownRoot_Throws()
    {
        using var reader = Fixture.CreateReader("<presets/>");
        Assert.Throws<InvalidOperationException>(() => LoadedResponse.Read(reader));
    }
}

public class ActionResponseTests
{
    [Fact]
    public void Read_Response_NotificationText()
    {
        using var reader = Fixture.Reader("ActionResponse.xml");
        var response = ActionResponse.Read(reader) as NotificationActionResponse;

        Assert.NotNull(response);
        Assert.Equal("Track added to queue", response.Text);
    }

    [Fact]
    public void Read_Back()
    {
        using var reader = Fixture.Reader("ActionBack.xml");
        Assert.IsType<BackActionResponse>(ActionResponse.Read(reader));
    }

    [Fact]
    public void Read_Skip()
    {
        using var reader = Fixture.Reader("ActionSkip.xml");
        Assert.IsType<SkipActionResponse>(ActionResponse.Read(reader));
    }

    [Fact]
    public void Read_Love()
    {
        using var reader = Fixture.Reader("ActionLove.xml");
        var response = ActionResponse.Read(reader) as LoveActionResponse;

        Assert.NotNull(response);
        Assert.Equal("1", response.Text);
        Assert.Null(response.Skip);
    }

    [Fact]
    public void Read_LoveWithSkipAttribute()
    {
        using var reader = Fixture.Reader("ActionLoveSkip.xml");
        var response = ActionResponse.Read(reader) as LoveActionResponse;

        Assert.NotNull(response);
        Assert.Equal("1", response.Skip);
        Assert.Equal("0", response.Text);
    }

    [Fact]
    public void Read_Ban()
    {
        using var reader = Fixture.Reader("ActionBan.xml");
        var response = ActionResponse.Read(reader) as BanActionResponse;

        Assert.NotNull(response);
        Assert.Equal("0", response.Text);
    }

    [Fact]
    public void Read_UnknownRoot_Throws()
    {
        using var reader = Fixture.CreateReader("<state>stream</state>");
        Assert.Throws<InvalidOperationException>(() => ActionResponse.Read(reader));
    }
}

public class BluChannelExceptionTests
{
    [Fact]
    public void ParseError_LoginToUseFavourites_UsesMessageElement()
    {
        // exact response seen for the "Add favourite" radio-station context-menu action
        using var reader = Fixture.CreateReader("""
            <?xml version="1.0" encoding="UTF-8"?>
            <error service="Airable"><message>Login to use favourites</message></error>
            """);

        var error = BluChannelException.ParseError(reader);

        Assert.Equal("Login to use favourites", error.Message);
    }

    [Fact]
    public void ParseError_MessageAndDetails_MessageElementWins()
    {
        using var reader = Fixture.CreateReader("""
            <error>
                <message>Invalid key</message>
                <detail>key value 'foobar' is not recognized</detail>
                <detail>hint: check the browseKey</detail>
            </error>
            """);

        var error = BluChannelException.ParseError(reader);

        Assert.Equal("Invalid key", error.Message);
    }

    [Fact]
    public void ParseError_WithoutMessageElement_FallsBack()
    {
        using var reader = Fixture.CreateReader("<error/>");

        var error = BluChannelException.ParseError(reader);

        Assert.Equal("The player returned an error", error.Message);
    }
}

public class FavouriteResponseTests
{
    [Fact]
    public void Read_DeleteFavouriteSuccess_ParsedWithoutError()
    {
        // exact response seen for the context-menu "Remove favorite" action (/DeleteFavourite);
        // accepted by the PlayURL dispatch so the action succeeds silently.
        using var reader = Fixture.CreateReader("""
            <?xml version="1.0" encoding="UTF-8"?>
            <favourite service="TuneIn">deleted</favourite>
            """);

        var response = FavouriteResponse.Read(reader);

        Assert.Equal("TuneIn", response.Service);
        Assert.Equal("deleted", response.Text);
    }
}

public class NotificationActionResponseTests
{
    [Fact]
    public void Read_ResponseRoot_TextParsed()
    {
        using var reader = Fixture.CreateReader("""
            <?xml version="1.0" encoding="UTF-8"?>
            <response>Track added to queue</response>
            """);

        var response = NotificationActionResponse.Read(reader);

        Assert.Equal("Track added to queue", response.Text);
        Assert.Equal("Notification: Track added to queue", response.ToString());
    }
}
