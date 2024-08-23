// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;

namespace CommunityToolkit.Datasync.Client.Offline.DeltaTokenStore;

/// <summary>
/// A default implementation of the <see cref="DeltaTokenStore"/> that will
/// store the delta tokens for each query in an Entity Framework table.
/// </summary>
internal class DefaultDeltaTokenStore(OfflineDbContext context) : IDeltaTokenStore
{
    /// <summary>
    /// Obtains the current delta token for a table/queryId from persistent store.
    /// </summary>
    /// <param name="queryId">The query ID of the table.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the delta token when complete.</returns>
    public async Task<DateTimeOffset> GetDeltaTokenAsync(string queryId, CancellationToken cancellationToken = default)
    {
        ValidateQueryId(queryId);
        DatasyncDeltaToken? deltaToken = await context.DatasyncDeltaTokens.FindAsync([queryId], cancellationToken).ConfigureAwait(false);
        long unixms = deltaToken?.Value ?? 0L;
        return DateTimeOffset.FromUnixTimeMilliseconds(unixms);
    }

    /// <summary>
    /// Resets the delta token for a table/queryId from persistent store.
    /// </summary>
    /// <param name="queryId">The query ID of the table.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the delta token has been reset.</returns>
    public async Task ResetDeltaTokenAsync(string queryId, CancellationToken cancellationToken = default)
    {
        ValidateQueryId(queryId);
        DatasyncDeltaToken? deltaToken = await context.DatasyncDeltaTokens.FindAsync([queryId], cancellationToken).ConfigureAwait(false);
        if (deltaToken is not null)
        {
            _ = context.DatasyncDeltaTokens.Remove(deltaToken);
        }
    }

    /// <summary>
    /// Sets the delta token for a table/queryId from persistent store.
    /// </summary>
    /// <param name="queryId">The query ID of the table.</param>
    /// <param name="value">The value of the delta token.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the delta token has been set in the persistent store.</returns>
    public async Task SetDeltaTokenAsync(string queryId, DateTimeOffset value, CancellationToken cancellationToken = default)
    {
        ValidateQueryId(queryId);
        long unixms = value.ToUnixTimeMilliseconds();
        DatasyncDeltaToken? deltaToken = await context.DatasyncDeltaTokens.FindAsync([queryId], cancellationToken).ConfigureAwait(false);
        if (deltaToken is null)
        {
            _ = context.DatasyncDeltaTokens.Add(new DatasyncDeltaToken() { Id = queryId, Value = unixms });
        }
        else if (deltaToken.Value != unixms)
        {
            deltaToken.Value = unixms;
            _ = context.DatasyncDeltaTokens.Update(deltaToken);
        }
    }

    /// <summary>
    /// Checks to see if the provided queryId is valid.
    /// </summary>
    /// <param name="queryId">The queryId to check.</param>
    /// <exception cref="ArgumentException">Thrown if the queryId is not valid.</exception>
    private static void ValidateQueryId(string queryId)
    {
        if (!EntityResolver.EntityIdIsValid(queryId))
        {
            throw new ArgumentException("Provided QueryId is not valid", nameof(queryId));
        }
    }
}
