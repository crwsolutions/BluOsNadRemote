using System;
using BluOsNadRemote.Blu4Net.Channel;
using Xunit;

namespace BluOsNadRemote.Blu4Net.Tests;

public class StatusResponseTests
{
    [Fact]
    public void Read_DocExample_AllFieldsPopulated()
    {
        using var reader = Fixture.Reader("Status.xml");
        var response = StatusResponse.Read(reader);

        Assert.Equal("4e266c9fbfba6d13d1a4d6ff4bd2e1e6", response.ETag);
        Assert.Equal("pause", response.State);
        Assert.Equal("MP3 320 kb/s", response.StreamFormat);
        Assert.Equal("320000", response.Quality);
        Assert.Equal(4, response.Volume);
        Assert.Equal(1, response.CanSeek);
        Assert.Equal(-50, response.Decibel);
        Assert.Equal("/Artwork?service=Deezer&songid=Deezer%3A142986206", response.Image);
        Assert.Equal("Deezer", response.Service);
        Assert.Equal("Ed Sheeran", response.Artist);
        Assert.Equal("1701", response.ArtistID);
        Assert.Equal("÷ (Deluxe)", response.Album);
        Assert.Equal("39197", response.AlbumID);
        Assert.Equal("Perfect", response.Title1);
        Assert.Equal("Ed Sheeran", response.Title2);
        Assert.Equal("÷ (Deluxe)", response.Title3);
        Assert.Equal(19, response.Song);
        Assert.Equal("Deezer:142986206", response.SongID);
        Assert.Equal(263, response.TotalLength);
        Assert.Equal(35, response.Seconds);
        Assert.Equal(0, response.Shuffle);
        Assert.Equal(2, response.Repeat);
        Assert.Equal("1054", response.PlaylistID);
        Assert.Equal("0", response.PresetsID);
        Assert.Null(response.IsPreset);
        Assert.Null(response.PresetName);
        Assert.Equal("/Sources/images/DeezerIcon.png", response.ServiceIcon);

        // undocumented elements (fn, name, cursor, ...) are ignored, <actions> absent → empty
        Assert.NotNull(response.Actions);
        Assert.Empty(response.Actions.Items);
    }

    [Fact]
    public void Read_EmptySongElement_SongIsNull()
    {
        using var reader = Fixture.Reader("StatusNoActions.xml");
        var response = StatusResponse.Read(reader);

        Assert.Null(response.Song);
        Assert.Equal("stream", response.State);
        Assert.Equal("Tidal:radio%3A%2F%2F443573", response.StreamUrl);
        Assert.Equal("2031", response.PlaylistID);
    }

    [Fact]
    public void Read_AdditionalTidalFields_UnknownElementsIgnored()
    {
        using var reader = Fixture.CreateReader("""
            <?xml version="1.0" encoding="UTF-8"?>
            <status etag="1c5af86e61b8812d17aa4afb6d545789">
              <album>From Q, With Love</album>
              <albumid>105506030</albumid>
              <artist>Patti Austin</artist>
              <artistid>1068</artistid>
              <canMovePlayback>true</canMovePlayback>
              <canSeek>1</canSeek>
              <cursor>55</cursor>
              <db>-100</db>
              <fn>Tidal:105506034</fn>
              <image>/Artwork?service=Tidal&amp;songid=Tidal%3A105506034</image>
              <indexing>0</indexing>
              <isFavourite>0</isFavourite>
              <mid>23</mid>
              <mode>1</mode>
              <mute>0</mute>
              <name>Baby, Come To Me</name>
              <pid>454</pid>
              <prid>0</prid>
              <quality>cd</quality>
              <repeat>2</repeat>
              <secs>12</secs>
              <service>Tidal</service>
              <serviceIcon>/Sources/images/TidalIcon.png</serviceIcon>
              <serviceName>TIDAL</serviceName>
              <serviceType>CloudService</serviceType>
              <shuffle>0</shuffle>
              <sid>5</sid>
              <similarstationid>Tidal:radio:artist/1068</similarstationid>
              <sleep></sleep>
              <song>49</song>
              <songid>Tidal:105506034</songid>
              <state>play</state>
              <streamFormat>FLAC 16/44.1</streamFormat>
              <syncStat>73</syncStat>
              <title1>Baby, Come To Me</title1>
              <title2>Patti Austin</title2>
              <title3>From Q, With Love</title3>
              <totlen>218</totlen>
              <trackstationid>Tidal:radio:track/105506034</trackstationid>
              <twoline_title1>Baby, Come To Me</twoline_title1>
              <twoline_title2>Patti Austin • From Q, With Love</twoline_title2>
              <volume>0</volume>
            </status>
            """);

        var response = StatusResponse.Read(reader);

        Assert.Equal("1c5af86e61b8812d17aa4afb6d545789", response.ETag);
        Assert.Equal("play", response.State);
        Assert.Equal("FLAC 16/44.1", response.StreamFormat);
        Assert.Equal("cd", response.Quality);
        Assert.Equal(0, response.Volume);
        Assert.Equal(1, response.CanSeek);
        Assert.Equal(-100, response.Decibel);
        Assert.Equal("/Artwork?service=Tidal&songid=Tidal%3A105506034", response.Image);
        Assert.Equal("Tidal", response.Service);
        Assert.Equal("Patti Austin", response.Artist);
        Assert.Equal("1068", response.ArtistID);
        Assert.Equal("From Q, With Love", response.Album);
        Assert.Equal("105506030", response.AlbumID);
        Assert.Equal("Baby, Come To Me", response.Title1);
        Assert.Equal("Patti Austin", response.Title2);
        Assert.Equal("From Q, With Love", response.Title3);
        Assert.Equal(49, response.Song);
        Assert.Equal("Tidal:105506034", response.SongID);
        Assert.Equal(218, response.TotalLength);
        Assert.Equal(12, response.Seconds);
        Assert.Equal(0, response.Shuffle);
        Assert.Equal(2, response.Repeat);
        Assert.Equal("454", response.PlaylistID);
        Assert.Equal("0", response.PresetsID);
        Assert.Null(response.IsPreset);
        Assert.Null(response.PresetName);
        Assert.Equal("/Sources/images/TidalIcon.png", response.ServiceIcon);

        Assert.NotNull(response.Actions);
        Assert.Empty(response.Actions.Items);
    }

    [Fact]
    public void Read_WithActions_ActionsParsed()
    {
        using var reader = Fixture.Reader("StatusWithActions.xml");
        var response = StatusResponse.Read(reader);

        Assert.Equal(4, response.Actions.Items.Length);
        Assert.Equal("back", response.Actions.Items[0].Name);
        Assert.Null(response.Actions.Items[0].Url);

        Assert.Equal("skip", response.Actions.Items[1].Name);
        Assert.Equal("/Action?service=Slacker&skip=4799148", response.Actions.Items[1].Url);

        var love = response.Actions.Items[2];
        Assert.Equal("love", love.Name);
        Assert.Equal("/images/loveban/love.png", love.Icon);
        Assert.Equal("Track marked as favorite", love.Notification);
        Assert.Equal("Love", love.Text);
        Assert.Equal("/Action?service=Slacker&love=4799148", love.Url);

        var ban = response.Actions.Items[3];
        Assert.Equal("ban", ban.Name);
        Assert.Equal("/Action?service=Slacker&ban=4799148", ban.Url);
    }
}

public class SyncStatusResponseTests
{
    [Fact]
    public void Read_DocExample_MasterAndSlavesParsed()
    {
        using var reader = Fixture.Reader("SyncStatus.xml");
        var response = SyncStatusResponse.Read(reader);

        Assert.Equal("23", response.ETag);
        Assert.Equal("PULSE", response.ModelName);
        Assert.Equal("PULSE-0278", response.Name);
        Assert.Equal("Bluesound", response.Brand);
        Assert.Equal(4, response.Volume);
        Assert.Equal(-76, response.Decibel);
        Assert.Equal("90:56:82:9F:02:78", response.MAC);
        Assert.False(response.IsZoneController);

        Assert.NotNull(response.Master);
        Assert.Equal(11000, response.Master.Port);
        Assert.Equal("192.168.1.100", response.Master.Address);

        Assert.Equal(2, response.Slave.Length);
        Assert.Equal("192.168.1.153", response.Slave[0].Address);
        Assert.Equal(11000, response.Slave[0].Port);
        Assert.Equal("192.168.1.234", response.Slave[1].Address);

        Assert.Null(response.ZoneSlave);
    }

    [Fact]
    public void Read_SlaveOnly_NoMaster()
    {
        using var reader = Fixture.Reader("SyncStatusSlaveOnly.xml");
        var response = SyncStatusResponse.Read(reader);

        Assert.Null(response.Master);
        var slave = Assert.Single(response.Slave);
        Assert.Equal("192.168.1.120", slave.Address);
    }

    [Fact]
    public void Read_ZoneWithZoneSlaveElement_AllZoneFieldsParsed()
    {
        using var reader = Fixture.Reader("SyncStatusZone.xml");
        var response = SyncStatusResponse.Read(reader);

        Assert.True(response.IsZoneController);
        Assert.Equal("Living Room", response.ZoneName);
        Assert.Equal("/ZoneUngroup?zone=Living%20Room", response.ZoneUngroupUrl);
        Assert.Equal("Left", response.ChannelName);
        Assert.Equal(ChannelMode.Left, response.ChannelMode);

        Assert.NotNull(response.ZoneSlave);
        Assert.Equal("192.168.1.51", response.ZoneSlave.Address);
        Assert.Equal(11001, response.ZoneSlave.Port);
        Assert.True(response.ZoneSlave.IsZoneSlave);
        Assert.Equal("Right", response.ZoneSlave.ChannelName);
        Assert.Equal(ChannelMode.Right, response.ZoneSlave.ChannelMode);
        Assert.Equal("Living Room Right", response.ZoneSlave.Name);
        Assert.Equal("C700", response.ZoneSlave.Model);
        Assert.Equal("C 700", response.ZoneSlave.ModelName);

        Assert.Empty(response.Slave);
        Assert.Null(response.Master);
    }

    [Fact]
    public void Read_EmptyBody_NoSlavesNoMaster()
    {
        using var reader = Fixture.Reader("SyncStatusEmpty.xml");
        var response = SyncStatusResponse.Read(reader);

        Assert.Empty(response.Slave);
        Assert.Null(response.Master);
        Assert.Null(response.ZoneSlave);
    }

    [Fact]
    public void Read_RootNameIsCaseSensitive()
    {
        using var reader = Fixture.CreateReader("<syncstatus etag=\"1\"/>");

        Assert.Throws<InvalidOperationException>(() => SyncStatusResponse.Read(reader));
    }
}

public class VolumeResponseTests
{
    [Fact]
    public void Read_SetVolume_AllFieldsParsed()
    {
        using var reader = Fixture.Reader("Volume.xml");
        var response = VolumeResponse.Read(reader);

        Assert.Equal(15, response.Volume);
        Assert.Equal(-49.9, response.Decibel);
        Assert.Equal(0, response.Mute);
        Assert.Equal("6213593a6132887e23fe0476b9ab2cba", response.ETag);
    }

    [Fact]
    public void Read_Muted()
    {
        using var reader = Fixture.Reader("VolumeMuted.xml");
        var response = VolumeResponse.Read(reader);

        Assert.Equal(0, response.Volume);
        Assert.Equal(-100, response.Decibel);
        Assert.Equal(1, response.Mute);
    }

    [Fact]
    public void Read_WithOffsetDb_OffsetIgnoredVolumeAndDbParsed()
    {
        using var reader = Fixture.Reader("VolumeWithOffsetDb.xml");
        var response = VolumeResponse.Read(reader);

        Assert.Equal(27, response.Volume);
        Assert.Equal(-25, response.Decibel);
        Assert.Equal(0, response.Mute);
    }
}

public class StateResponseTests
{
    [Theory]
    [InlineData("StatePlay.xml", "play")]
    [InlineData("StatePause.xml", "pause")]
    [InlineData("StateStream.xml", "stream")]
    public void Read_AllStates(string fixture, string expected)
    {
        using var reader = Fixture.Reader(fixture);
        var response = StateResponse.Read(reader);
        Assert.Equal(expected, response.State);
    }
}

public class IdDeleteSavedResponseTests
{
    [Fact]
    public void Read_Id()
    {
        using var reader = Fixture.Reader("Id.xml");
        Assert.Equal(21, IdResponse.Read(reader).ID);
    }

    [Fact]
    public void Read_Deleted()
    {
        using var reader = Fixture.Reader("Deleted.xml");
        Assert.Equal(9, DeleteResponse.Read(reader).ID);
    }

    [Fact]
    public void Read_Saved()
    {
        using var reader = Fixture.Reader("Saved.xml");
        Assert.Equal(126, SavedResponse.Read(reader).Entries);
    }
}
