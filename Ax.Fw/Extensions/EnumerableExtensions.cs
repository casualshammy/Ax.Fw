﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions
{
    public static class EnumerableExtensions
    {
        public static void Do<T>(this IEnumerable<T> _enumerable, Action<T> _action)
        {
            foreach (T item in _enumerable)
                _action(item);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
        {
            if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
            if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
            foreach (var element in _enumerable)
            {
                if (await _predicate(element)) return element;
            }
            return default;
        }

        public static async Task<bool> AnyAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
        {
            if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
            if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
            foreach (var element in _enumerable)
            {
                if (await _predicate(element)) return true;
            }
            return false;
        }

        public static async Task<bool> AllAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
        {
            if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
            if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
            foreach (var element in _enumerable)
            {
                if (!await _predicate(element)) return false;
            }
            return true;
        }

        public static async Task<T> FirstAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
        {
            if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
            if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
            foreach (var element in _enumerable)
            {
                if (await _predicate(element)) return element;
            }
            throw new InvalidOperationException("Element not found");
        }

        public static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(this IEnumerable<TSource> _source, Func<TSource, Task<TResult>> _selector)
        {
            if (_source == null)
                throw new ArgumentNullException(nameof(_source));

            if (_selector == null)
                throw new ArgumentNullException(nameof(_selector));

            foreach (var entry in _source)
                yield return await _selector(entry);
        }

        public static async IAsyncEnumerable<TSource> WhereAsync<TSource>(this IEnumerable<TSource> _source, Func<TSource, Task<bool>> _selector)
        {
            if (_source == null)
                throw new ArgumentNullException(nameof(_source));

            if (_selector == null)
                throw new ArgumentNullException(nameof(_selector));

            foreach (var entry in _source)
                if (await _selector(entry))
                    yield return entry;
        }

    }
}
