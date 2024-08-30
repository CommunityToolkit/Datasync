// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of extension methods for the <see cref="IAsyncEnumerable{T}"/> interface.
/// </summary>
/// <remarks>
/// These are mostly duplicative of the ones in System.Linq.Async, but are repeated
/// here so we don't have to take a dependency on a third party library that we don't
/// control.
/// </remarks>
internal static class IAsyncEnumerableExtensions
{
    /// <summary>
    /// Creates an array from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source async-enumerable sequence to get an array of elements for.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element with an array containing all the elements of the source sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    internal static async ValueTask<TSource[]> ToDatasyncArrayAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        List<TSource> list = await source.ToDatasyncListAsync(cancellationToken).ConfigureAwait(false);
        return [.. list];
    }

    /// <summary>
    /// Creates a dictionary from an async-enumerable sequence according to a specified key selector function.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the dictionary key computed for each element in the source sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence to create a dictionary for.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element with a dictionary mapping unique key values onto the corresponding source sequence's element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    internal static ValueTask<Dictionary<TKey, TSource>> ToDatasyncDictionaryAsync<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default) where TKey : notnull
        => ToDatasyncDictionaryAsync(source, keySelector, comparer: null, cancellationToken);

    /// <summary>
    /// Creates a dictionary from an async-enumerable sequence according to a specified key selector function, and a comparer.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the dictionary key computed for each element in the source sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence to create a dictionary for.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">An equality comparer to compare keys.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element with a dictionary mapping unique key values onto the corresponding source sequence's element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    internal static async ValueTask<Dictionary<TKey, TSource>> ToDatasyncDictionaryAsync<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, CancellationToken cancellationToken = default) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        Dictionary<TKey, TSource> d = new(comparer);
        await foreach (TSource? item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            TKey key = keySelector(item);
            d.Add(key, item);
        }

        return d;
    }

    /// <summary>
    /// Creates a hash set from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source async-enumerable sequence to get a hash set of elements for.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element with a hash set containing all the elements of the source sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    internal static ValueTask<HashSet<TSource>> ToDatasyncHashSetAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        => ToDatasyncHashSetAsync(source, comparer: null, cancellationToken);

    /// <summary>
    /// Creates a hash set from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source async-enumerable sequence to get a hash set of elements for.</param>
    /// <param name="comparer">An equality comparer to compare elements of the sequence.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element with a hash set containing all the elements of the source sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    internal static async ValueTask<HashSet<TSource>> ToDatasyncHashSetAsync<TSource>(this IAsyncEnumerable<TSource> source, IEqualityComparer<TSource>? comparer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        HashSet<TSource> set = new(comparer);
        await foreach (TSource? item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            _ = set.Add(item);
        }

        return set;
    }

    /// <summary>
    /// Creates a list from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source async-enumerable sequence to get a list of elements for.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A list containing all the elements of the source sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    internal static async ValueTask<List<TSource>> ToDatasyncListAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        List<TSource> list = [];
        await foreach (TSource? item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }

    /// <summary>
    /// Creates a <see cref="ConcurrentObservableCollection{T}"/> from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source async-enumerable sequence to get a list of elements for.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An observable collection containing all the elements of the source sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    internal static ValueTask<ConcurrentObservableCollection<TSource>> ToDatasyncObservableCollectionAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        => source.ToDatasyncObservableCollectionAsync([], cancellationToken);

    /// <summary>
    /// Updates a <see cref="ConcurrentObservableCollection{T}"/> from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source async-enumerable sequence to get a list of elements for.</param>
    /// <param name="existingCollection">The existing observable collection object.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>The updated observable collection containing all the elements of the source sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    internal static async ValueTask<ConcurrentObservableCollection<TSource>> ToDatasyncObservableCollectionAsync<TSource>(this IAsyncEnumerable<TSource> source, ConcurrentObservableCollection<TSource> existingCollection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        List<TSource> list = await source.ToDatasyncListAsync(cancellationToken).ConfigureAwait(false);
        existingCollection.ReplaceAll(list);
        return existingCollection;
    }
}

