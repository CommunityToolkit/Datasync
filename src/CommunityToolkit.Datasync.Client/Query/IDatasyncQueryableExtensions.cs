// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of extension methods for the <see cref="IDatasyncQueryable{TEntity}"/> interface.
/// </summary>
public static class IDatasyncQueryableExtensions
{
    /// <summary>
    /// Asynchronously returns the number of elements that would be returned by the query.
    /// </summary>
    /// <typeparam name="TSource">The type of the entity being counted.</typeparam>
    /// <param name="query">The query to use for counting.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of elements that would be returned by the query.</returns>
    public static async ValueTask<int> CountAsync<TSource>(this IDatasyncQueryable<TSource> query, CancellationToken cancellationToken = default) where TSource : class
    {
        ServiceResponse<int> response = await query.ServiceClient.CountAsync(query, new DatasyncServiceOptions(), cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    /// <summary>
    /// Asynchronously returns the number of elements that would be returned by the query.
    /// </summary>
    /// <typeparam name="TSource">The type of the entity being counted.</typeparam>
    /// <param name="query">The query to use for counting.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of elements that would be returned by the query.</returns>
    public static async ValueTask<long> LongCountAsync<TSource>(this IDatasyncQueryable<TSource> query, CancellationToken cancellationToken = default) where TSource : class
    {
        ServiceResponse<long> response = await query.ServiceClient.LongCountAsync(query, new DatasyncServiceOptions(), cancellationToken ).ConfigureAwait(false);
        return response.Value;
    }

    /// <summary>
    /// Creates an array from the result of a table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An array of the results.</returns>
    public static ValueTask<TSource[]> ToArrayAsync<TSource>(this IDatasyncQueryable<TSource> query, CancellationToken cancellationToken = default) where TSource : class
        => query.ToAsyncEnumerable().ToDatasyncArrayAsync(cancellationToken);

    /// <summary>
    /// Executes a query on the remote service, returning the results as an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the result set.</typeparam>
    /// <param name="query">The query to execute on the remote service..</param>
    /// <returns>The async-enumerable sequence whose elements are pulled from the result set.</returns>
    public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IDatasyncQueryable<TSource> query) where TSource : class
        => query.ServiceClient.Query(query);

    /// <summary>
    /// Executes a query on the remote service, returning the results as an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the result set.</typeparam>
    /// <param name="query">The query to execute on the remote service..</param>
    /// <returns>The async-enumerable sequence whose elements are pulled from the result set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is null.</exception>
    public static IAsyncPageable<TSource> ToAsyncPageable<TSource>(this IDatasyncQueryable<TSource> query) where TSource : class
        => query.ServiceClient.Query(query);

    /// <summary>
    /// Creates a dictionary from the result of a table query according to a specified key selector function.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the dictionary key computed for each element in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A dictionary mapping unique key values onto the corresponding result's element.</returns>
    public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IDatasyncQueryable<TSource> query, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default) 
        where TKey : notnull 
        where TSource : class
        => query.ToAsyncEnumerable().ToDatasyncDictionaryAsync(keySelector, cancellationToken);

    /// <summary>
    /// Creates a dictionary from the result of a table query according to a specified key selector function.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the dictionary key computed for each element in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">An equality comparer to compare keys.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A dictionary mapping unique key values onto the corresponding result's element.</returns>
    public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IDatasyncQueryable<TSource> query, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, CancellationToken cancellationToken = default) 
        where TKey : notnull
        where TSource : class
        => query.ToAsyncEnumerable().ToDatasyncDictionaryAsync(keySelector, comparer, cancellationToken);

    /// <summary>
    /// Creates a hash set from the results of a table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A hash set containing all the elements of the source sequence.</returns>
    public static ValueTask<HashSet<TSource>> ToHashSetAsync<TSource>(this IDatasyncQueryable<TSource> query, CancellationToken cancellationToken = default) where TSource : class
        => query.ToAsyncEnumerable().ToDatasyncHashSetAsync(cancellationToken);

    /// <summary>
    /// Creates a hash set from the results of a table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="comparer">An equality comparer to compare elements of the sequence.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A hash set containing all the elements of the source sequence.</returns>
    public static ValueTask<HashSet<TSource>> ToHashSetAsync<TSource>(this IDatasyncQueryable<TSource> query, IEqualityComparer<TSource>? comparer, CancellationToken cancellationToken = default) where TSource: class
        => query.ToAsyncEnumerable().ToDatasyncHashSetAsync(comparer, cancellationToken);

    /// <summary>
    /// Creates a list from the result set of the table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A list containing all the elements of the source sequence.</returns>
    public static ValueTask<List<TSource>> ToListAsync<TSource>(this IDatasyncQueryable<TSource> query, CancellationToken cancellationToken = default) where TSource : class
        => query.ToAsyncEnumerable().ToDatasyncListAsync(cancellationToken);

    /// <summary>
    /// Creates a <see cref="ConcurrentObservableCollection{T}"/> from the result set of the table query
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A <see cref="ConcurrentObservableCollection{T}"/> containing all the elements of the source sequence.</returns>
    public static ValueTask<ConcurrentObservableCollection<TSource>> ToObservableCollectionAsync<TSource>(this IDatasyncQueryable<TSource> query, CancellationToken cancellationToken = default) where TSource : class
        => query.ToAsyncEnumerable().ToDatasyncObservableCollectionAsync(cancellationToken);

    /// <summary>
    /// Updates a <see cref="ConcurrentObservableCollection{T}"/> from the result set of the table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="query">The source table query.</param>
    /// <param name="collection">The <see cref="ConcurrentObservableCollection{T}"/> to update.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>The <see cref="ConcurrentObservableCollection{T}"/> passed in containing all the elements of the source sequence (replacing the old content).</returns>
    public static ValueTask<ConcurrentObservableCollection<TSource>> ToObservableCollectionAsync<TSource>(this IDatasyncQueryable<TSource> query, ConcurrentObservableCollection<TSource> collection, CancellationToken cancellationToken = default) where TSource : class
        => query.ToAsyncEnumerable().ToDatasyncObservableCollectionAsync(collection, cancellationToken);
}
