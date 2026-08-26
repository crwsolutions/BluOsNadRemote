using System;
using System.Xml;

namespace BluOsNadRemote.Blu4Net.Channel;

public class ActionResponse
{
    /// <summary>
    /// Dispatches on the root element name. Note: the player returns either <c>love</c> or a
    /// player-specific root (e.g. <c>ban</c>) depending on the action. Throws
    /// <see cref="InvalidOperationException"/> for any other root, mirroring the previous
    /// attribute-based deserializer behaviour.
    /// </summary>
    internal static ActionResponse Read(XmlReader reader)
    {
        return reader.LocalName switch
        {
            "response" => NotificationActionResponse.Read(reader),
            "back" => BackActionResponse.Read(reader),
            "skip" => SkipActionResponse.Read(reader),
            "ban" => BanActionResponse.Read(reader),
            "love" => LoveActionResponse.Read(reader),
            _ => throw new InvalidOperationException($"Encountered invalid xml root element <{reader.LocalName}>")
        };
    }
}

public sealed class NotificationActionResponse : ActionResponse
{
    public string Text;

    new internal static NotificationActionResponse Read(XmlReader reader)
    {
        reader.ReadRoot("response");
        return new NotificationActionResponse
        {
            Text = reader.ReadText(),
        };
    }

    public override string ToString()
    {
        return $"Notification: {Text}";
    }
}

public sealed class BackActionResponse : ActionResponse
{
    new internal static BackActionResponse Read(XmlReader reader)
    {
        reader.ReadRoot("back");
        return new BackActionResponse();
    }

    public override string ToString()
    {
        return "back";
    }
}

public sealed class SkipActionResponse : ActionResponse
{
    new internal static SkipActionResponse Read(XmlReader reader)
    {
        reader.ReadRoot("skip");
        return new SkipActionResponse();
    }

    public override string ToString()
    {
        return "skip";
    }
}

public sealed class BanActionResponse : ActionResponse
{
    public string Text;

    new internal static BanActionResponse Read(XmlReader reader)
    {
        reader.ReadRoot("ban");
        return new BanActionResponse
        {
            Text = reader.ReadText(),
        };
    }

    public override string ToString()
    {
        return $"ban {Text}";
    }
}

public sealed class LoveActionResponse : ActionResponse
{
    public string Skip;

    public string Text;

    new internal static LoveActionResponse Read(XmlReader reader)
    {
        reader.ReadRoot("love");
        return new LoveActionResponse
        {
            Skip = reader.Attr("skip"),
            Text = reader.ReadText(),
        };
    }

    public override string ToString()
    {
        return $"Love: {Text}";
    }
}
