﻿using System.Diagnostics.CodeAnalysis;
using System.Threading;

#pragma warning disable

namespace Omni.Core
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// This is a micro-cache; suitable when the number of terms is controllable (a few hundred, for example),
        /// and strictly append-only; you cannot change existing values. All key matches are on **REFERENCE**
        /// equality. The type is fully thread-safe.
        /// </summary>
        /// <typeparam name="TKey">The type to cache.</typeparam>
        /// <typeparam name="TValue">The value type of the cache.</typeparam>
        internal class Link<TKey, TValue> where TKey : class
        {
            public static void Clear(ref Link<TKey, TValue>? head) => Interlocked.Exchange(ref head, null);
            public static bool TryGet(Link<TKey, TValue>? link, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                while (link is not null)
                {
                    if ((object)key == (object)link.Key)
                    {
                        value = link.Value!;
                        return true;
                    }
                    link = link.Tail;
                }
                value = default;
                return false;
            }

            public static bool TryAdd(ref Link<TKey, TValue>? head, TKey key, ref TValue value)
            {
                bool tryAgain;
                do
                {
                    var snapshot = Interlocked.CompareExchange(ref head, null, null);
                    if (TryGet(snapshot, key, out TValue? found))
                    { // existing match; report the existing value instead
                        value = found;
                        return false;
                    }
                    var newNode = new Link<TKey, TValue>(key, value, snapshot);
                    // did somebody move our cheese?
                    tryAgain = Interlocked.CompareExchange(ref head, newNode, snapshot) != snapshot;
                } while (tryAgain);
                return true;
            }

            private Link(TKey key, TValue value, Link<TKey, TValue>? tail)
            {
                Key = key;
                Value = value;
                Tail = tail;
            }

            public TKey Key { get; }
            public TValue Value { get; }
            public Link<TKey, TValue>? Tail { get; }
        }
    }
}
