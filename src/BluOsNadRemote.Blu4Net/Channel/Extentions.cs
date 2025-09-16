using BluOsNadRemote.Blu4Net.Channel;
using System;
using System.Reactive.Linq;

namespace BluOsNadRemote.Blu4Net.Channel;

internal static class Extentions
{
    internal static IObservable<T> Retry<T>(this IObservable<T> src, TimeSpan delay)
    {
        if (delay == TimeSpan.Zero) return src.Retry();
        return src.Catch(src.DelaySubscription(delay).Retry());
    }
}
