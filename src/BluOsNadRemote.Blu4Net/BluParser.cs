﻿using System;

namespace BluOsNadRemote.Blu4Net;

public static class BluParser
{
    public static Uri ParseAbsoluteUri(string value, Uri baseUri)
    {
        if (!string.IsNullOrEmpty(value) && Uri.TryCreate(value, value?.StartsWith("/") == true ? UriKind.Relative : UriKind.RelativeOrAbsolute, out var uri))
        {
            if (uri.IsAbsoluteUri)
            {
                return uri;
            }
            return new Uri(baseUri, uri);
        }
        return null;
    }

    public static PlayerState ParseState(string value)
    {
        if (value != null)
        {
            switch (value)
            {
                case "stream":
                    return PlayerState.Streaming;
                case "play":
                    return PlayerState.Playing;
                case "pause":
                    return PlayerState.Paused;
                case "stop":
                    return PlayerState.Stopped;
                case "connecting":
                    return PlayerState.Connecting;
            }
        }
        return PlayerState.Unknown;
    }

    public static PlayerAction ParseAction(string value)
    {
        if (value != null)
        {
            switch (value)
            {
                case "back":
                    return PlayerAction.Back;
                case "skip":
                    return PlayerAction.Skip;
                case "love":
                    return PlayerAction.Love;
                case "ban":
                    return PlayerAction.Ban;
            }
        }
        return PlayerAction.Unknown;
    }
}
