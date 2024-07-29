// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Client.Service;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// Represents a query that can be evaluated against an OData table.
/// </summary>
/// <remarks>
/// Rather than implenting <see cref="IQueryable{T}"/> directly, we've implemented the portion of  the LINQ query
/// pattern we support on a datasync service.  You can use the <see cref="IDatasyncQueryable{TEntity}"/> instance
/// to build up a query using normal LINQ patterns.
/// </remarks>
internal class DatasyncQueryable<TEntity> : IDatasyncQueryable<TEntity>
{
    /// <summary>
    /// Creates a new instance of the <see cref="DatasyncQueryable{TEntity}"/> class.
    /// </summary>
    /// <param name="service">The remote service that is being used.</param>
    /// <param name="query">The current <see cref="IQueryable{T}"/>.</param>
    /// <param name="parameters">The additional query parameters to send.</param>
    /// <param name="requestTotalCount">If <c>true</c>, request a total count of items that will be returned without paging.</param>
    internal DatasyncQueryable(IReadOnlyDatasyncServiceClient<TEntity> service, IQueryable<TEntity> query, IDictionary<string, string> parameters, bool requestTotalCount)
    {
        ServiceClient = service;
        Queryable = query;
        QueryParameters = parameters;
        RequestTotalCount = requestTotalCount;
    }

    /// <summary>
    /// The service connection for this query.
    /// </summary>
    public IReadOnlyDatasyncServiceClient<TEntity> ServiceClient { get; }

    /// <summary>
    /// The user-defined query string parameters to include with the query when sent to the remote service.
    /// </summary>
    public IDictionary<string, string> QueryParameters { get; }

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> associated with this query.
    /// </summary>
    public IQueryable<TEntity> Queryable { get; internal set; }

    /// <summary>
    /// If <c>true</c>, include the total count of items that will be returned with this query (without considering paging).
    /// </summary>
    public bool RequestTotalCount { get; internal set; }

    /// <summary>
    /// Ensure the query will get the deleted records.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> IncludeDeletedItems(bool enabled = true)
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
    public IDatasyncQueryable<TEntity> IncludeTotalCount(bool enabled = true)
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
    public IDatasyncQueryable<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        Queryable = Queryable.OrderBy(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        Queryable = Queryable.OrderByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified selection to the source query.
    /// </summary>
    /// <typeparam name="U">Type representing the projected result of the query.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<U> Select<U>(Expression<Func<TEntity, U>> selector)
        => new DatasyncQueryable<U>(ServiceClient.ToServiceClient<U>(), Queryable.Select(selector), QueryParameters, RequestTotalCount);

    /// <summary>
    /// Applies the specified skip clause to the source query.
    /// </summary>
    /// <param name="count">The number to skip.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0, nameof(count));
        Queryable = Queryable.Skip(count);
        return this;
    }

    /// <summary>
    /// Applies the specified take clause to the source query.
    /// </summary>
    /// <param name="count">The number to take.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0, nameof(count));
        Queryable = Queryable.Take(count);
        return this;
    }

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        Queryable = ((IOrderedQueryable<TEntity>)Queryable).ThenBy(keySelector);
        return this;
    }

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        Queryable = ((IOrderedQueryable<TEntity>)Queryable).ThenByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// Converts the current query into an OData v4 query string.
    /// </summary>
    /// <returns></returns>
    public string ToODataQueryString()
        => new QueryTranslator<TEntity>(this).Translate().ToODataQueryString();

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        Queryable = Queryable.Where(predicate);
        return this;
    }

    /// <summary>
    /// Adds the parameter to the list of user-defined parameters to send with the request.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> WithParameter(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        if (key.StartsWith('$') || key.StartsWith("__"))
        {
            throw new ArgumentException($"Parameter '{key}' is invalid.", nameof(key));
        }

        QueryParameters[key] = value.Trim();
        return this;
    }

    /// <summary>
    /// Applies to the source query the specified string key-value pairs to be used as user-defined parameters with the request URI query string.
    /// </summary>
    /// <param name="parameters">The parameters to apply.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        parameters.ToList().ForEach(parameter => _ = WithParameter(parameter.Key, parameter.Value));
        return this;
    }
}
