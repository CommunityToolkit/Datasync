// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// An interface that describes the LINQ methods that a remote datasync service supports.
/// </summary>
/// <typeparam name="T">The type of the data transer object (DTO) or model being used for the query.</typeparam>
public interface IDatasyncLinqMethods<T>
{
        /// <summary>
        /// Ensure the query will get the deleted records.
        /// </summary>
        /// <param name="enabled">If <c>true</c>, enables this request.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> IncludeDeletedItems(bool enabled = true);

        /// <summary>
        /// Ensure the query will get the total count for all the records that would have been returned
        /// ignoring any take paging/limit clause specified by client or server.
        /// </summary>
        /// <param name="enabled">If <c>true</c>, enables this requst.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> IncludeTotalCount(bool enabled = true);

        /// <summary>
        /// Applies the specified ascending order clause to the source query.
        /// </summary>
        /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
        /// <param name="keySelector">The expression selecting the member to order by.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Applies the specified descending order clause to the source query.
        /// </summary>
        /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
        /// <param name="keySelector">The expression selecting the member to order by.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Applies the specified selection to the source query.
        /// </summary>
        /// <typeparam name="U">Type representing the projected result of the query.</typeparam>
        /// <param name="selector">The selector function.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<U> Select<U>(Expression<Func<T, U>> selector);

        /// <summary>
        /// Applies the specified skip clause to the source query.
        /// </summary>
        /// <param name="count">The number to skip.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> Skip(int count);

        /// <summary>
        /// Applies the specified take clause to the source query.
        /// </summary>
        /// <param name="count">The number to take.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> Take(int count);

        /// <summary>
        /// Applies the specified ascending order clause to the source query.
        /// </summary>
        /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
        /// <param name="keySelector">The expression selecting the member to order by.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Applies the specified descending order clause to the source query.
        /// </summary>
        /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
        /// <param name="keySelector">The expression selecting the member to order by.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Applies the specified filter predicate to the source query.
        /// </summary>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds the parameter to the list of user-defined parameters to send with the
        /// request.
        /// </summary>
        /// <param name="key">The parameter key</param>
        /// <param name="value">The parameter value</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> WithParameter(string key, string value);

        /// <summary>
        /// Applies to the source query the specified string key-value
        /// pairs to be used as user-defined parameters with the request URI
        /// query string.
        /// </summary>
        /// <param name="parameters">The parameters to apply.</param>
        /// <returns>The composed query object.</returns>
        IDatasyncRemoteQuery<T> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters);

        /// <summary>
        /// Count the number of items that would be returned by the provided query, without returning all the values.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A task that returns the number of items that will be in the result set when the query finishes.</returns>
        ValueTask<long> LongCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the result of the query as an <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <returns>The list of items as an <see cref="IAsyncEnumerable{T}"/></returns>
        IAsyncEnumerable<T> ToAsyncEnumerable();
}