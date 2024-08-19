// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// Represents a query that can be evaluated against an OData table for pull operations.
/// </summary>
/// <remarks>
/// Rather than implenting <see cref="IQueryable{T}"/> directly, we've implemented the portion of the LINQ query
/// pattern we support on a datasync service.  You can use the <see cref="IDatasyncQueryable{TEntity}"/>
/// instance to build up a query using normal LINQ patterns.
/// </remarks>
public interface IDatasyncPullQuery<TEntity> where TEntity : class
{
    /// <summary>
    /// The user-defined query string parameters to include with the query when sent to the remote service.
    /// </summary>
    IDictionary<string, string> QueryParameters { get; }

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> associated with this query.
    /// </summary>
    IQueryable<TEntity> Queryable { get; }

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncPullQuery<TEntity> Where(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds the parameter to the list of user-defined parameters to send with the request.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>The composed query object.</returns>
    IDatasyncPullQuery<TEntity> WithParameter(string key, string value);

    /// <summary>
    /// Applies to the source query the specified string key-value pairs to be used as user-defined parameters with the request URI query string.
    /// </summary>
    /// <param name="parameters">The parameters to apply.</param>
    /// <returns>The composed query object.</returns>
    IDatasyncPullQuery<TEntity> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters);
}
