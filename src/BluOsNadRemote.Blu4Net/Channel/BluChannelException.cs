using System;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

/// <summary>
/// Thrown by <see cref="BluChannel"/> when the player answers with an <c>&lt;error&gt;</c> root
/// element (e.g. <c>&lt;error service="Airable"&gt;&lt;message&gt;Login to use favourites&lt;/message&gt;&lt;/error&gt;</c>).
/// The message is the player-provided user-readable message, e.g. "Login to use favourites".
/// </summary>
public class BluChannelException : Exception
{
    public BluChannelException(string message, Exception innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Parses an <c>&lt;error&gt;</c> response into an exception.
    /// Uses the <c>&lt;message&gt;</c> child element as the message when present,
    /// otherwise falls back to the raw content of the error element.
    /// </summary>
    internal static BluChannelException ParseError(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        reader.ReadRoot("error");

        string message = null;

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    break;
                }
                continue;
            }

            if (reader.LocalName == "message")
            {
                message = reader.ReadText();
            }
            else
            {
                reader.Skip();
            }
        }

        return new BluChannelException(string.IsNullOrWhiteSpace(message) ? "The player returned an error" : message);
    }
}
