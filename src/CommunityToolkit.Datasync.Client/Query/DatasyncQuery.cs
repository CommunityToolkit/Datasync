// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// Represents a query that can be evaluated against an OData-based datasync endpoint.
/// </summary>
/// <remarks>
/// Rather than implementing <see cref="IQueryable{T}"/> directly, we've implemented the portion of
/// the LINQ query pattern we support on the datasync service.  You can use the <see cref="IDatasyncQueryable{T}"/>
/// instance to build up a query using normal LINQ patterns.
/// </remarks>
/// <typeparam name="T">The type of entity that is expected to be returned by the datasync service.</typeparam>
internal class DatasyncQueryable<T> : IDatasyncQueryable<T>
{
    /// <summary>
    /// Creates a new <see cref="DatasyncQueryable{T}"/> instance that will reference a specific URI
    /// </summary>
    /// <param name="relativeUri">The relative URI to the datasync table.</param>
    internal DatasyncQueryable(Uri relativeUri) 
        : this(relativeUri, Array.Empty<T>().AsQueryable(), new Dictionary<string, string>(), false)
    {
    }

    /// <summary>
    /// Creates a new <see cref="DatasyncQueryable{T}"/> based on the provided information.
    /// </summary>
    /// <param name="relativeUri">The relative URI to the datasync table.</param>
    /// <param name="query">The underlying <see cref="IQueryable{T}"/> that is associated with this query.</param>
    /// <param name="queryParameters">A set of user-defined query paramters to include with the query.</param>
    /// <param name="requestTotalCount">If <c>true</c>, request the total count of items that will be returned with this query.</param>
    internal DatasyncQueryable(Uri relativeUri, IQueryable<T> query, IDictionary<string, string> queryParameters, bool requestTotalCount)
    {
        Query = Ensure.That(query, nameof(query)).IsNotNull().Value;
        QueryParameters = Ensure.That(queryParameters, nameof(queryParameters)).IsNotNull().Value;
        RelativeUri = Ensure.That(relativeUri, nameof(relativeUri)).IsHttpPath().Value;
        RequestTotalCount = requestTotalCount;
    }

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> that is associated with this query.
    /// </summary>
    public IQueryable<T> Query { get; internal set; }

    /// <summary>
    /// A set of user-defined query paramters to include when the query is sent
    /// to the remote datasync service.
    /// </summary>
    public IDictionary<string, string> QueryParameters { get; internal set; }

    /// <summary>
    /// The relative URI to the datasync table endpoint.
    /// </summary>
    public Uri RelativeUri { get; internal set; }

    /// <summary>
    /// If <c>true</c>, request the total count of items that will be returned with this query
    /// (without considering paging).
    /// </summary>
    public bool RequestTotalCount { get; internal set; }

    /// <summary>
    /// Converts this query to a query string that is transmitted to the service.
    /// </summary>
    /// <returns></returns>
    public string ToDatasyncQueryString()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Ensure the query will get the deleted records.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<T> IncludeDeletedItems(bool enabled = true)
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
    public IDatasyncQueryable<T> IncludeTotalCount(bool enabled = true)
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
    public IDatasyncQueryable<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
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
    public IDatasyncQueryable<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
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
    public IDatasyncQueryable<U> Select<U>(Expression<Func<T, U>> selector)
    {
        return new DatasyncQueryable<U>(RelativeUri, Query.Select(selector), QueryParameters, RequestTotalCount);
    }

    /// <summary>
    /// Applies the specified skip clause to the source query.
    /// </summary>
    /// <param name="count">The number to skip.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<T> Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0, nameof(count));
        Query = Query.Skip(count);
        return this;
    }

    /// <summary>
    /// Applies the specified take clause to the source query.
    /// </summary>
    /// <param name="count">The number to take.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<T> Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 0, nameof(count));
        Query = Query.Take(count);
        return this;
    }

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
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
    public IDatasyncQueryable<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = ((IOrderedQueryable<T>)Query).ThenByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<T> Where(Expression<Func<T, bool>> predicate)
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
    public IDatasyncQueryable<T> WithParameter(string key, string value)
    {
        _ = Ensure.That(key, nameof(key)).IsNotNullOrWhiteSpace().And.IsHttpQueryParameter();
        _ = Ensure.That(value, nameof(value)).IsNotNullOrWhiteSpace();
        QueryParameters[key] = value;
        return this;
    }

    /// <summary>
    /// Applies to the source query the specified string key-value
    /// pairs to be used as user-defined parameters with the request URI
    /// query string.
    /// </summary>
    /// <param name="parameters">The parameters to apply.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<T> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        parameters.ToList().ForEach(param => _ = WithParameter(param.Key, param.Value));
        return this;
    }
}