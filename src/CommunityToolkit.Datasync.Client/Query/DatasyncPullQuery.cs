// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// Represents a query that can be evaluated against an OData table.
/// </summary>
/// <remarks>
/// Rather than implenting <see cref="IQueryable{T}"/> directly, we've implemented the portion of  the LINQ query
/// pattern we support on a datasync service.  You can use the <see cref="IDatasyncQueryable{TEntity}"/> instance
/// to build up a query using normal LINQ patterns.
/// </remarks>
internal class DatasyncPullQuery<TEntity> : IDatasyncPullQuery<TEntity> where TEntity : class
{
    /// <summary>
    /// The user-defined query string parameters to include with the query when sent to the remote service.
    /// </summary>
    public IDictionary<string, string> QueryParameters { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> associated with this query.
    /// </summary>
    public IQueryable<TEntity> Queryable { get; internal set; } = Array.Empty<TEntity>().AsQueryable();

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncPullQuery<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
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
    public IDatasyncPullQuery<TEntity> WithParameter(string key, string value)
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
    public IDatasyncPullQuery<TEntity> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        parameters.ToList().ForEach(parameter => _ = WithParameter(parameter.Key, parameter.Value));
        return this;
    }
}
