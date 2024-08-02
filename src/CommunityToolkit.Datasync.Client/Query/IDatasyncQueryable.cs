// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Client.Service;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// Represents a query that can be evaluated against an OData table.
/// </summary>
/// <remarks>
/// Rather than implenting <see cref="IQueryable{T}"/> directly, we've implemented the portion of the LINQ query
/// pattern we support on a datasync service.  You can use the <see cref="IDatasyncQueryable{TEntity}"/>
/// instance to build up a query using normal LINQ patterns.
/// </remarks>
public interface IDatasyncQueryable<TEntity> : IDatasyncQueryMethods<TEntity> where TEntity : class
{
    /// <summary>
    /// The service connection for this query.
    /// </summary>
    IReadOnlyDatasyncServiceClient<TEntity> ServiceClient { get; }

    /// <summary>
    /// The user-defined query string parameters to include with the query when sent to the remote service.
    /// </summary>
    IDictionary<string, string> QueryParameters { get; }

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> associated with this query.
    /// </summary>
    IQueryable<TEntity> Queryable { get; }

    /// <summary>
    /// If <c>true</c>, include the total count of items that will be returned with this query (without considering paging).
    /// </summary>
    bool RequestTotalCount { get; }

    /// <summary>
    /// Converts the current query into an OData v4 query string.
    /// </summary>
    /// <returns>An OData query string.</returns>
    string ToODataQueryString();
}
