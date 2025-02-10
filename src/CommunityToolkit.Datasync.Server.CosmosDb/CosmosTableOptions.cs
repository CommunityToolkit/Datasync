// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Server.CosmosDb;

/// <summary>
/// Base implementation of <see cref="ICosmosTableOptions{TEntity}"/>
/// </summary>
/// <typeparam name="TEntity">The <see cref="CosmosTableData"/> that these options apply to.</typeparam>
public abstract class CosmosTableOptions<TEntity> : ICosmosTableOptions<TEntity> where TEntity : CosmosTableData
{
    /// <inheritdoc />
    public virtual string DatabaseId { get; }
    /// <inheritdoc />
    public virtual string ContainerId { get; }
    /// <inheritdoc />
    public virtual bool ShouldUpdateTimestamp { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="CosmosTableOptions{TEntity}"/> class.
    /// </summary>
    /// <param name="databaseId">The ID of the database that the container is in.</param>
    /// <param name="containerId">The ID of the container that the entities are stored in.</param>
    /// <param name="shouldUpdateTimestamp">Should the timestamp be updated when an entity is updated by the repository (default is true).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseId"/> or <paramref name="containerId"/> are null or whitespace.</exception>"
    public CosmosTableOptions(
        string databaseId,
        string containerId,
        bool shouldUpdateTimestamp = true)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
        {
            throw new ArgumentException($"'{nameof(databaseId)}' cannot be null or whitespace.", nameof(databaseId));
        }

        if (string.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException($"'{nameof(containerId)}' cannot be null or whitespace.", nameof(containerId));
        }

        DatabaseId = databaseId;
        ContainerId = containerId;
        ShouldUpdateTimestamp = shouldUpdateTimestamp;
    }
    /// <inheritdoc />
    public virtual Func<TEntity, string> IdGenerator => (_) => Guid.NewGuid().ToString();
    /// <inheritdoc />
    public virtual bool TryParsePartitionKey(string entityId, out string id, out PartitionKey partitionKey)
    {
        try
        {
            id = ParsePartitionKey(entityId, out PartitionKey pk);
            partitionKey = pk;
            return true;
        }
        catch
        {
            id = string.Empty;
            partitionKey = default;
            return false;
        }
    }
    /// <inheritdoc />
    public abstract string ParsePartitionKey(string id, out PartitionKey partitionKey);
    /// <inheritdoc />
    public abstract string GetPartitionKey(TEntity entity, out PartitionKey partitionKey);
    /// <inheritdoc />
    public abstract Expression<Func<TEntity, bool>> QueryablePredicate();
}