// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;

namespace CommunityToolkit.Datasync.Client.Table;

/// <summary>
/// The interface that shows support for querying a remote datasync service.
/// </summary>
public interface IRemoteDbSet
{
    /// <summary>
    /// Returns a long count of the entities that match the query.
    /// </summary>
    /// <typeparam name="T">The type of the entity being queried.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the number of entities that match the query.</returns>
    ValueTask<long> LongCountAsync<T>(IDatasyncRemoteQuery<T> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the results of the query.
    /// </summary>
    /// <typeparam name="T">The type of the entity being queried.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> for the entities being returned.</returns>
    IAsyncEnumerable<T> ToAsyncEnumerable<T>(IDatasyncRemoteQuery<T> query);
}