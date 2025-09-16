using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace BluOsNadRemote.Blu4Net;

internal static partial class Extentions
{
    internal static IObservable<TResult> SelectAsync<T, TResult>(this IObservable<T> source, Func<T, Task<TResult>> selector)
    {
        return source
            .Select(value => Observable.FromAsync(() => selector(value)))
            .Concat();
    }
}
