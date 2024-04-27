// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.OData;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// An OData LINQ query.
/// </summary>
/// <remarks>
/// Rather than implementing the representative subset of LINQ methods that can be translated to OData, and
/// then expanding for the extra couple of things we need, we've implemented the portion of the LINQ query
/// patterm we support on a datasync service.
/// </remarks>
/// <typeparam name="T">The entity type being queried.</typeparam>
internal class ODataQuery<T> : IODataQuery<T>
{
    internal ODataQuery(IReadonlyRemoteDataset<T> dataset, IQueryable<T> query, IDictionary<string, string> queryParameters, bool requestTotalCount)
    {
        Dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
        Query = query ?? Array.Empty<T>().AsQueryable();
        QueryParameters = queryParameters ?? new Dictionary<string, string>();
        RequestTotalCount = requestTotalCount;
    }

    #region IODataQuery<T> implementation
    /// <summary>
    /// The remote dataset that is used by this query.
    /// </summary>
    public IReadonlyRemoteDataset<T> Dataset { get; }

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> describing the query to be sent to the remote service.
    /// </summary>
    public IQueryable<T> Query { get; internal set; }

    /// <summary>
    /// The user-defined query string parameters to include with the query when sent to the remote service.
    /// </summary>
    public IDictionary<string, string> QueryParameters { get; }

    /// <summary>
    /// If <c>true</c>, the total count of items that will be returned with this query will be requested.
    /// </summary>
    public bool RequestTotalCount { get; set; }
    #endregion

    #region IODataLinqMethods<T> implementation
    /// <summary>
    /// Ensure the query will get the deleted records.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> IncludeDeletedItems(bool enabled = true)
    {
        if (enabled)
        {
            QueryParameters[ODataQueryParameters.IncludeDeleted] = "true";
        }
        else
        {
            _ = QueryParameters.Remove(ODataQueryParameters.IncludeDeleted);
        }

        return this;
    }

    /// <summary>
    /// Ensure the query will get the total count for all the records that would have been returned
    /// ignoring any take paging/limit clause specified by client or server.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this requst.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> IncludeTotalCount(bool enabled = true)
    {
        RequestTotalCount = enabled;
        return this;
    }

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = Query.OrderBy(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = Query.OrderByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified selection to the source query.
    /// </summary>
    /// <typeparam name="U">Type representing the projected result of the query.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<U> Select<U>(Expression<Func<T, U>> selector)
        => new ODataQuery<U>(Dataset.AsDataset<U>(), Query.Select(selector), QueryParameters, RequestTotalCount);

    /// <summary>
    /// Applies the specified skip clause to the source query.
    /// </summary>
    /// <param name="count">The number to skip.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> Skip(int count)
    {
        Ensure.That(count, nameof(count)).IsGte(0);
        Query = Query.Skip(count);
        return this;
    }

    /// <summary>
    /// Applies the specified take clause to the source query.
    /// </summary>
    /// <param name="count">The number to take.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> Take(int count)
    {
        Ensure.That(count, nameof(count)).IsGt(0);
        Query = Query.Take(count);
        return this;
    }

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = ((IOrderedQueryable<T>)Query).ThenBy(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = ((IOrderedQueryable<T>)Query).ThenByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        Query = Query.Where(predicate);
        return this;
    }

    /// <summary>
    /// Adds the parameter to the list of user-defined parameters to send with the
    /// request.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> WithParameter(string key, string value)
    {
        Ensure.That(key, nameof(key)).IsValidHttpHeaderName();
        Ensure.That(value?.Trim(), nameof(value)).IsNotNullOrEmpty();
        QueryParameters[key.ToLowerInvariant()] = Uri.EscapeDataString(value);
        return this;
    }
    #endregion
}
