// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// The set of LINQ methods that can be used with an OData query.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
public interface IODataQuery<T> : IODataLinqMethods<T>
{
    /// <summary>
    /// The remote dataset that is used by this query.
    /// </summary>
    IReadonlyRemoteDataset<T> Dataset { get; }

    /// <summary>
    /// The underlying <see cref="IQueryable{T}"/> describing the query to be sent to the remote service.
    /// </summary>
    IQueryable<T> Query { get; }

    /// <summary>
    /// The user-defined query string parameters to include with the query when sent to the remote service.
    /// </summary>
    IDictionary<string, string> QueryParameters { get; }

    /// <summary>
    /// If <c>true</c>, the total count of items that will be returned with this query will be requested.
    /// </summary>
    bool RequestTotalCount { get; }
}
