using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Tests;

internal static class Fixture
{
    /// <summary>
    /// Returns the full path of a test-data XML file (copied next to the test assembly at build time).
    /// </summary>
    public static string Path(string fileName)
    {
        return System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
    }

    /// <summary>
    /// Creates an <see cref="XmlReader"/> positioned on the root element of the given test-data file.
    /// </summary>
    public static XmlReader Reader(string fileName)
    {
        var xml = File.ReadAllText(Path(fileName));
        return CreateReader(xml);
    }

    /// <summary>
    /// Creates an <see cref="XmlReader"/> positioned on the root element of the given XML string.
    /// </summary>
    public static XmlReader CreateReader(string xml)
    {
        var reader = XmlReader.Create(new StringReader(xml));
        reader.MoveToContent();
        return reader;
    }
}
