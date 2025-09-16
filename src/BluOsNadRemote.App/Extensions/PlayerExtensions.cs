using BluOsNadRemote.Blu4Net;

namespace BluOsNadRemote.App.Extensions;

internal static class PlayerExtensions
{
    internal static RepeatMode ToNextRepeatMode(this RepeatMode currentMode)
    {
        if (currentMode == RepeatMode.RepeatOff)
        {
            return RepeatMode.RepeatOne;
        }

        if (currentMode == RepeatMode.RepeatOne)
        {
            return RepeatMode.RepeatAll;
        }

        return RepeatMode.RepeatOff;
    }

    internal static ShuffleMode ToNextShuffleMode(this ShuffleMode currentMode)
    {
        if (currentMode == ShuffleMode.ShuffleOff)
        {
            return ShuffleMode.ShuffleOn;
        }

        return ShuffleMode.ShuffleOff;
    }

    internal static bool ToPlayerCanBeStarted(this PlayerState playerState)
    {
        if (playerState == PlayerState.Playing || playerState == PlayerState.Streaming)
        {
            return false;
        }

        return true;
    }
}
