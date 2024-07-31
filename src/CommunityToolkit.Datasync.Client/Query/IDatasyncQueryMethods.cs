// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// The set of LINQ query methods that can be applied to a <see cref="IDatasyncQueryable{TEntity}"/>
/// or an <see cref="IDatasyncServiceClient{TEntity}"/> for querying a remote datasync service.
/// </summary>
/// <typeparam name="TEntity">The type of entity being queried.</typeparam>
public interface IDatasyncQueryMethods<TEntity> where TEntity : class
{
    /// <summary>
    /// Ensure the query will get the deleted records.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> IncludeDeletedItems(bool enabled = true);

    /// <summary>
    /// Ensure the query will get the total count for all the records that would have been returned
    /// ignoring any take paging/limit clause specified by client or server.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this requst.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> IncludeTotalCount(bool enabled = true);

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Applies the specified selection to the source query.
    /// </summary>
    /// <typeparam name="U">Type representing the projected result of the query.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<U> Select<U>(Expression<Func<TEntity, U>> selector) where U : class;

    /// <summary>
    /// Applies the specified skip clause to the source query.
    /// </summary>
    /// <param name="count">The number to skip.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> Skip(int count);

    /// <summary>
    /// Applies the specified take clause to the source query.
    /// </summary>
    /// <param name="count">The number to take.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> Take(int count);

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds the parameter to the list of user-defined parameters to send with the request.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> WithParameter(string key, string value);

    /// <summary>
    /// Applies to the source query the specified string key-value pairs to be used as user-defined parameters with the request URI query string.
    /// </summary>
    /// <param name="parameters">The parameters to apply.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncQueryable<TEntity> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters);
}
