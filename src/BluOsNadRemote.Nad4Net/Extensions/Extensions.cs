using System;
using System.Reactive.Linq;

namespace Nad4Net.Extensions
{
    internal static class Extensions
    {
        public static IObservable<T> Retry<T>(this IObservable<T> src, TimeSpan delay)
        {
            if (delay == TimeSpan.Zero) return src.Retry();
            return src.Catch(src.DelaySubscription(delay).Retry());
        }
    }
}
