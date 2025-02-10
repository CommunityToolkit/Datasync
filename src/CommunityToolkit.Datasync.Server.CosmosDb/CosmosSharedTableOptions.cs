// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Server.CosmosDb;
/// <summary>
/// Implementation of <see cref="ICosmosTableOptions{TEntity}" /> that supports storing multiple entity types in the same container.
/// Defaults the partition key to the entity type name.
/// </summary>
/// <typeparam name="TEntity">The <see cref="CosmosTableData{T}"/> that these options applys to.</typeparam>
public class CosmosSharedTableOptions<TEntity> : CosmosTableOptions<TEntity> where TEntity : CosmosTableData<TEntity>
{
    /// <summary>
    /// Creates a new instance of the <see cref="CosmosSharedTableOptions{TEntity}" /> class.
    /// </summary>
    /// <param name="databaseId">The ID of the database that the container is in.</param>
    /// <param name="containerId">The ID of the container that the entities are stored in.</param>
    /// <param name="shouldUpdateTimestamp">Should the timestamp be updated when an entity is updated by the repository (default is true).</param>
    public CosmosSharedTableOptions(string databaseId, string containerId, bool shouldUpdateTimestamp = true) : base(databaseId, containerId, shouldUpdateTimestamp)
    {
        Entity = typeof(TEntity).Name;
    }   
    /// <summary>
    /// The entity type for the data. Used as the default partition key for shared containers. defaults to the entity type name.
    /// </summary>
    public virtual string Entity { get; }
    /// <summary>
    /// Gets the partition key for the entity from the entity. Defaults to the entity type name.
    /// </summary>
    /// <param name="entity">The <see cref="CosmosTableData{T}"/> to retrieve the partition key from.</param>
    /// <param name="partitionKey">A <see cref="PartitionKey"/> containing the type name.</param>
    /// <returns>The original id of the entity.</returns>

    public override string GetPartitionKey(TEntity entity, out PartitionKey partitionKey)
    {
        partitionKey = new PartitionKey(Entity);
        return entity.Id;
    }
    /// <summary>
    /// Parses the partition key from the id. Defaults to the entity type name.
    /// </summary>
    /// <param name="id">The id of the entity</param>
    /// <param name="partitionKey">A <see cref="PartitionKey"/> containing the entity type name.</param>
    /// <returns>The id passed in the id parameter.</returns>
    public override string ParsePartitionKey(string id, out PartitionKey partitionKey)
    {
        partitionKey = new PartitionKey(Entity);
        return id;
    }
    /// <summary>
    /// Returns a predicate that will filters the data by the entity name.
    /// </summary>
    /// <returns></returns>
    public override Expression<Func<TEntity, bool>> QueryablePredicate() => (e) => e.Entity == Entity;
}
