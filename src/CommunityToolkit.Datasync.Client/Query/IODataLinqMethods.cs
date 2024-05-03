// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// An interface describing the LINQ methods that a remote dataset can use.
/// </summary>
/// <typeparam name="T">The type of the entity being queried.</typeparam>
public interface IODataLinqMethods<T>
{
    /// <summary>
    /// Ensure the query will get the deleted records.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> IncludeDeletedItems(bool enabled = true);

    /// <summary>
    /// Ensure the query will get the total count for all the records that would have been returned
    /// ignoring any take paging/limit clause specified by client or server.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this requst.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> IncludeTotalCount(bool enabled = true);

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Applies the specified selection to the source query.
    /// </summary>
    /// <typeparam name="U">Type representing the projected result of the query.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<U> Select<U>(Expression<Func<T, U>> selector);

    /// <summary>
    /// Applies the specified skip clause to the source query.
    /// </summary>
    /// <param name="count">The number to skip.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> Skip(int count);

    /// <summary>
    /// Applies the specified take clause to the source query.
    /// </summary>
    /// <param name="count">The number to take.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> Take(int count);

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> Where(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Adds the parameter to the list of user-defined parameters to send with the
    /// request.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>The composed query object.</returns>
    IODataQuery<T> WithParameter(string key, string value);

    /// <summary>
    /// Count the number of entities that would be returned by the query.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The number of entities that would be returned by the query.</returns>
    ValueTask<long> LongCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query on the remote service, allowing the enumeration of the results asynchronously.
    /// </summary>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> for enumerating the entities asynchronously.</returns>
    IAsyncEnumerable<T> ToAsyncEnumerable();
}
