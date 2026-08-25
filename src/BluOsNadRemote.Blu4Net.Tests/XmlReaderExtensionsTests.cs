using System;
using System.Xml;
using BluOsNadRemote.Blu4Net.Channel;
using Xunit;

namespace BluOsNadRemote.Blu4Net.Tests;

public class XmlReaderExtensionsTests
{
    [Fact]
    public void Attr_MissingAttribute_ReturnsNull()
    {
        using var reader = Fixture.CreateReader("<root other=\"x\"/>");
        Assert.Null(reader.Attr("missing"));
        Assert.Equal("x", reader.Attr("other"));
    }

    [Fact]
    public void AttrInt_MissingAttribute_ReturnsZero()
    {
        using var reader = Fixture.CreateReader("<root/>");
        Assert.Equal(0, reader.AttrInt("missing"));
    }

    [Fact]
    public void AttrDouble_MissingAttribute_ReturnsZero()
    {
        using var reader = Fixture.CreateReader("<root/>");
        Assert.Equal(0.0, reader.AttrDouble("missing"));
    }

    [Fact]
    public void AttrBool_Variants_ParsedCorrectly()
    {
        using var reader = Fixture.CreateReader("<root a=\"true\" b=\"1\" c=\"false\" d=\"0\"/>");
        Assert.True(reader.AttrBool("a"));
        Assert.True(reader.AttrBool("b"));
        Assert.False(reader.AttrBool("c"));
        Assert.False(reader.AttrBool("d"));
        Assert.False(reader.AttrBool("missing"));
    }

    [Fact]
    public void ReadText_TrimsWhitespace()
    {
        using var reader = Fixture.CreateReader("<root>  hello world  </root>");
        Assert.Equal("hello world", reader.ReadText());
    }

    [Fact]
    public void ReadText_EmptyElement_ReturnsEmptyString()
    {
        using var reader = Fixture.CreateReader("<root><song></song></root>");
        reader.Read(); // move onto <song>
        Assert.Equal("", reader.ReadText());
    }

    [Fact]
    public void ReadInt_EmptyElement_ReturnsZero()
    {
        using var reader = Fixture.CreateReader("<root><value></value></root>");
        reader.Read(); // move onto <value>
        Assert.Equal(0, reader.ReadInt());
    }

    [Fact]
    public void ReadInt_InvalidValue_ThrowsFormatExceptionWithElementName()
    {
        using var reader = Fixture.CreateReader("<root><volume>abc</volume></root>");
        reader.Read(); // move onto <volume>

        var error = Assert.Throws<FormatException>(() => reader.ReadInt());
        Assert.Contains("<volume>", error.Message);
    }

    [Fact]
    public void ReadDouble_UsesInvariantCulture()
    {
        using var reader = Fixture.CreateReader("<root><db>-49.9</db></root>");
        reader.Read(); // move onto <db>
        Assert.Equal(-49.9, reader.ReadDouble());
    }

    [Fact]
    public void ReadIntOrNullOrThrow_EmptyElement_ReturnsNull()
    {
        using var reader = Fixture.CreateReader("<root><song></song></root>");
        reader.Read(); // move onto <song>
        Assert.Null(reader.ReadIntOrNullOrThrow());
    }

    [Fact]
    public void ReadRoot_MatchingRoot_ReturnsReader()
    {
        using var reader = Fixture.CreateReader("<browse/>");
        Assert.Same(reader, reader.ReadRoot("browse"));
    }

    [Fact]
    public void ReadRoot_MismatchedRoot_Throws()
    {
        using var reader = Fixture.CreateReader("<status/>");

        var error = Assert.Throws<InvalidOperationException>(() => reader.ReadRoot("SyncStatus"));
        Assert.Contains("<status>", error.Message);
    }
}
