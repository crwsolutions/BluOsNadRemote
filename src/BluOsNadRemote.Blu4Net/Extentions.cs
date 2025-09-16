﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Blu4Net
{
    public static partial class Extentions
    {
        public static IObservable<TResult> SelectAsync<T, TResult>(this IObservable<T> source, Func<T, Task<TResult>> selector)
        {
            return source
                .Select(value => Observable.FromAsync(() => selector(value)))
                .Concat();
        }
    }
}
