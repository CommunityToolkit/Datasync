// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The delta token store holds the last sync times for a specific query.
/// </summary>
internal interface IDeltaTokenStore
{
    /// <summary>
    /// Obtains the current delta token for a queryId from persistent store.
    /// </summary>
    /// <param name="queryId">The query ID of the table.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the delta token when complete.</returns>
    Task<DateTimeOffset> GetDeltaTokenAsync(string queryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the delta token for a queryId from persistent store.
    /// </summary>
    /// <param name="queryId">The query ID of the table.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the delta token has been reset.</returns>
    Task ResetDeltaTokenAsync(string queryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the delta token for a queryId from persistent store.
    /// </summary>
    /// <param name="queryId">The query ID of the table.</param>
    /// <param name="value">The value of the delta token.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the delta token has been set in the persistent store.</returns>
    Task<bool> SetDeltaTokenAsync(string queryId, DateTimeOffset value, CancellationToken cancellationToken = default);
}
