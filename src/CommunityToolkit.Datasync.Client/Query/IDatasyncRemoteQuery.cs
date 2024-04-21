// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Table;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// Represents a query that can be evaluated against an OData table.
/// </summary>
/// <remarks>
/// A remote datasync service only supports a subset of the LINQ methods available,
/// but also supports a couple of extra requirements as well.  By making our own
/// queryable instead of using IQueryable{T}, we can support the set and provide
/// design-time feedback on compatibility.
/// </remarks>
public interface IDatasyncRemoteQuery<T> : IDatasyncLinqMethods<T>
{
    /// <summary>
    /// The <see cref="IRemoteDbSet"/> for the ddatasync service.
    /// </summary>
    /// <remarks>
    /// This is normally provided via a <see cref="IRemoteDbSet"/> connection.
    /// </remarks>
    IRemoteDbSet RemoteDbSet { get; }

    /// <summary>
    /// The user-defined query string parameters to include with the query when
    /// sent to the remote service.
    /// </summary>
    IDictionary<string, string> Parameters { get; }

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> associated with this query.
    /// </summary>
    IQueryable<T> Query { get; set; }

    /// <summary>
    /// If <c>true</c>, include the total count of items that will be returned with this query
    /// (without considering paging).
    /// </summary>
    bool RequestTotalCount { get; } 
}