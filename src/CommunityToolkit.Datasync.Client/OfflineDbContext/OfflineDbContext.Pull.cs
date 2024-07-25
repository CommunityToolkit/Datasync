// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Context;
using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Models;
using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Common;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An <see cref="OfflineDbContext"/> instance represents a combination of the Unit of Work
/// and Repository patterns such that it can be used to query a local database, just like a
/// <see cref="DbContext"/> instance.  It is further extended to provide data synchronization
/// capabilities between the on-client database and a remote service.
/// </summary>
/// <remarks>
/// Each <see cref="DbSet{TEntity}"/> should be independent and not contain foreign relationships
/// so that the data synchronization process can push and pull tables independently.
/// </remarks>
public abstract partial class OfflineDbContext : DbContext
{
    /// <summary>
    /// The storage for the delta tokens for each query.
    /// </summary>
    public DbSet<SynchronizationSetMetadata> SynchronizationSetMetadata => Set<SynchronizationSetMetadata>();

    /// <summary>
    /// Creates an absolute URI for the request based on the base address from the <see cref="HttpClient"/>
    /// that is being used and the relative URI that is passed with the query.
    /// </summary>
    /// <param name="baseAddress">The (optional) absolute URI on the HttpClient.</param>
    /// <param name="relativeUri">The absolute or relative URI from the query.</param>
    /// <returns>The (guaranteed) absolute URI for the datasync endpoint.</returns>
    /// <exception cref="UriFormatException">Thrown if we cannot make an absolute URI.</exception>
    internal static Uri GetAbsoluteUriForDatasyncService(Uri? baseAddress, Uri relativeUri)
    {
        if (relativeUri.IsAbsoluteUri)
        {
            return Ensure.That(relativeUri, nameof(relativeUri)).IsDatasyncEndpoint().Value;
        }

        if (baseAddress == null)
        {
            throw new UriFormatException("You must specify a BaseAddress in your HttpClient when using relative URIs");
        }

        return new Uri(baseAddress, relativeUri);
    }

    /// <summary>
    /// Returns the <see cref="SynchronizationSetMetadata"/> for the current entity type and query ID.  The Query ID is provided
    /// through the options, and may be empty.
    /// </summary>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="queryId">The query ID for this entity type.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The <see cref="SynchronizationSetMetadata"/> for the query, or null if it doesn't exist.</returns>
    internal async ValueTask<SynchronizationSetMetadata?> GetSynchronizationSetMetadata(Type entityType, string queryId, CancellationToken cancellationToken = default)
    {
        string baseId = entityType.Name.ToLowerInvariant();
        string synchronizationSetId = string.IsNullOrWhiteSpace(queryId) ? baseId : $"{baseId}#{queryId.Trim()}";
        return await SynchronizationSetMetadata.FindAsync([synchronizationSetId], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The main pull operation for data synchronization.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to pull from the server.</typeparam>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation when complete.</returns>
    public ValueTask<PullResult> PullAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
        => PullAsync<TEntity>(new PullOperationOptions(), cancellationToken);

    /// <summary>
    /// The main pull operation for data synchronization.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to pull from the server.</typeparam>
    /// <param name="query">The query to execute on the remote service.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation when complete.</returns>
    public ValueTask<PullResult> PullAsync<TEntity>(IDatasyncQueryable<TEntity> query, CancellationToken cancellationToken = default) where TEntity : class
        => PullAsync(query, new PullOperationOptions(), cancellationToken);

    /// <summary>
    /// The main pull operation for data synchronization.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to pull from the server.</typeparam>
    /// <param name="options">The options to use in executing the pull operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation when complete.</returns>
    public ValueTask<PullResult> PullAsync<TEntity>(PullOperationOptions options, CancellationToken cancellationToken = default) where TEntity : class
    {
        if (!DatasyncContext.EntityContextMap.TryGetValue(typeof(TEntity), out DatasyncEntityContext? entityContext))
        {
            throw new DatasyncException($"Invalid entity type for data synchronization: ${typeof(TEntity).Name}");
        }

        return PullAsync(new DatasyncQueryable<TEntity>(entityContext.Endpoint), options, cancellationToken);
    }

    /// <summary>
    /// The main pull operation for data synchronization.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to pull from the server.</typeparam>
    /// <param name="query">The query to execute on the remote service.</param>
    /// <param name="options">The options to use in executing the pull operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation when complete.</returns>
    public async ValueTask<PullResult> PullAsync<TEntity>(IDatasyncQueryable<TEntity> query, PullOperationOptions options, CancellationToken cancellationToken = default) where TEntity : class
    {
        if (!DatasyncContext.EntityContextMap.TryGetValue(typeof(TEntity), out DatasyncEntityContext? entityContext))
        {
            throw new DatasyncException($"Invalid entity type for data synchronization: ${typeof(TEntity).Name}");
        }

        // Get the OData Client for this request
        IDatasyncClient<TEntity> datasyncClient = DatasyncContext.CreateDatasyncClient<TEntity>();
        PullResult result = new();

        // Retrieve the SynchronizationSetMetadata for the entity type or query operation.
        SynchronizationSetMetadata? metadata = await GetSynchronizationSetMetadata(typeof(TEntity), options.QueryId, cancellationToken).ConfigureAwait(false);
        DateTimeOffset deltaToken = metadata?.DeltaToken ?? DateTimeOffset.UnixEpoch;

        // Execute the query, returning an async enumerable of pages.
        DatasyncQueryOptions queryOptions = new() { RetrieveEntitiesUpdatedSince = metadata?.DeltaToken };
        IAsyncEnumerable<Page<TEntity>> pages = datasyncClient.Query(query, queryOptions).AsPages();

        // Loop over each page, updating the database context for the new records
        await foreach(Page<TEntity> page in pages)
        {
            //  - force inject each entity into the table (over-writing anything that is there)
            //  - update the synchronizationsetmetadata for the table
            //  - savechangesasync
            //  - update the pullresult
        }

        return result;
    }
}
