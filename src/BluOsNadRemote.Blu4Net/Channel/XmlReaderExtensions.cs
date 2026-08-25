using System;
using System.Globalization;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

internal static class XmlReaderExtensions
{
    /// <summary>
    /// Reads the value of the given attribute. Returns <see langword="null"/> when the attribute is absent.
    /// </summary>
    public static string Attr(this XmlReader reader, string name)
    {
        var value = reader.GetAttribute(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Validates that the current element is the expected root element name (case-sensitive),
    /// mirroring the root validation of the previous deserializer. Returns the reader unchanged
    /// on success.
    /// </summary>
    public static XmlReader ReadRoot(this XmlReader reader, string rootName)
    {
        if (reader.LocalName != rootName)
        {
            throw new InvalidOperationException($"Encountered invalid xml root element <{reader.LocalName}>");
        }

        return reader;
    }

    /// <summary>
    /// Reads an integer attribute using the invariant culture. Returns <c>0</c> (type default) when the
    /// attribute is absent or empty, matching the previous attribute-based deserializer behaviour.
    /// </summary>
    public static int AttrInt(this XmlReader reader, string name)
    {
        var value = reader.Attr(name);
        return value != null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    /// <summary>
    /// Reads a double attribute using the invariant culture. Returns <c>0.0</c> (type default) when the
    /// attribute is absent or empty, matching the previous attribute-based deserializer behaviour.
    /// </summary>
    public static double AttrDouble(this XmlReader reader, string name)
    {
        var value = reader.Attr(name);
        return value != null && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.0;
    }

    /// <summary>
    /// Reads a boolean attribute. Returns <c>false</c> when the attribute is absent or empty.
    /// </summary>
    public static bool AttrBool(this XmlReader reader, string name)
    {
        var value = reader.Attr(name);
        return value is { } v && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1");
    }

    /// <summary>
    /// Reads the text content of the current element (direct text nodes only; nested elements are
    /// skipped). Returns <see langword="null"/> when the element contains no (non-whitespace) text.
    /// The reader ends positioned on the current element's <c>EndElement</c>, so the surrounding
    /// loop can detect the element's end.
    /// </summary>
    private static string ReadContentText(this XmlReader reader)
    {
        // self-closing element: no content; leave the reader on the element so the
        // surrounding loop's Read() advances to the next node
        if (reader.IsEmptyElement)
        {
            return null;
        }

        var result = new System.Text.StringBuilder();

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.EndElement:
                    // end of the element whose content is being read
                    return result.Length > 0 ? result.ToString() : null;
                case XmlNodeType.Element:
                    reader.Skip();
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    result.Append(reader.Value);
                    break;
            }
        }

        throw new InvalidOperationException($"Unexpected end of xml inside element <{reader.Name}>");
    }

    /// <summary>
    /// Reads the text content of the current element, trimmed. Returns <c>""</c> for empty elements.
    /// </summary>
    public static string ReadText(this XmlReader reader)
    {
        return reader.ReadContentText()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Reads the text content of the current element as an <see cref="int"/> using the invariant culture.
    /// An absent or empty element yields the type default (<c>0</c>), matching the previous attribute-based deserializer behaviour.
    /// Throws <see cref="FormatException"/> (with the element name) for invalid values.
    /// </summary>
    public static int ReadInt(this XmlReader reader)
    {
        var elementName = reader.Name;
        var text = reader.ReadContentText();

        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new FormatException($"Invalid integer value '{text.Trim()}' for element <{elementName}>");
    }

    /// <summary>
    /// Reads the text content of the current element as a nullable <see cref="int"/> using the invariant culture.
    /// An absent, empty or whitespace-only element yields <see langword="null"/> (used for <c>int?</c> fields
    /// such as <see cref="StatusResponse.Song"/>). Throws <see cref="FormatException"/> for invalid values.
    /// </summary>
    public static int? ReadIntOrNullOrThrow(this XmlReader reader)
    {
        var elementName = reader.Name;
        var text = reader.ReadContentText();

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new FormatException($"Invalid integer value '{text.Trim()}' for element <{elementName}>");
    }

    /// <summary>
    /// Reads the text content of the current element as a <see cref="double"/> using the invariant culture.
    /// An absent or empty element yields the type default (<c>0</c>), matching the previous attribute-based deserializer behaviour.
    /// Throws <see cref="FormatException"/> (with the element name) for invalid values.
    /// </summary>
    public static double ReadDouble(this XmlReader reader)
    {
        var elementName = reader.Name;
        var text = reader.ReadContentText();

        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new FormatException($"Invalid numeric value '{text.Trim()}' for element <{elementName}>");
    }
}
