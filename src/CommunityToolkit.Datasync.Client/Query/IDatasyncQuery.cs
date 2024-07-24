// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// Represents a query that can be evaluated against an OData-based datasync endpoint.
/// </summary>
/// <remarks>
/// Rather than implementing <see cref="IQueryable{T}"/> directly, we've implemented the portion of
/// the LINQ query pattern we support on the datasync service.  You can use the <see cref="IDatasyncQueryable{T}"/>
/// instance to build up a query using normal LINQ patterns.
/// </remarks>
/// <typeparam name="T">The type of entity that is expected to be returned by the datasync service.</typeparam>
public interface IDatasyncQueryable<T> : IDatasyncQueryMethods<T>
{
    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> that is associated with this query.
    /// </summary>
    IQueryable<T> Query { get; }

    /// <summary>
    /// A set of user-defined query paramters to include when the query is sent
    /// to the remote datasync service.
    /// </summary>
    IDictionary<string, string> QueryParameters { get; }

    /// <summary>
    /// The relative URI to the datasync table endpoint.
    /// </summary>
    Uri RelativeUri { get; }

    /// <summary>
    /// If <c>true</c>, request the total count of items that will be returned with this query
    /// (without considering paging).
    /// </summary>
    bool RequestTotalCount { get; }

    /// <summary>
    /// Converts this query to a query string that is transmitted to the service.
    /// </summary>
    /// <param name="updatedAt">If provided, generate a query that is suitable for a synchronization event for records since this value.</param>
    /// <returns>The OData query string representing the query.</returns>
    string ToDatasyncQueryString(DateTimeOffset? updatedAt);
}
