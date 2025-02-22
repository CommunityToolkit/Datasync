// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Server.CosmosDb;
/// <summary>
/// Implementation of <see cref="CosmosTableOptions{TEntity}"/> for a single table. Defaults the partition key to the entity's ID.
/// </summary>
/// <typeparam name="TEntity">The <see cref="CosmosTableData" /> that these options apply to.</typeparam>
public class CosmosSingleTableOptions<TEntity> : CosmosTableOptions<TEntity> where TEntity : CosmosTableData
{
    /// <summary>
    /// Creates a new instance of the <see cref="CosmosSingleTableOptions{TEntity}" /> class.
    /// </summary>
    /// <param name="databaseId">The ID of the database that the container is in.</param>
    /// <param name="containerId">The ID of the container that the entities are stored in.</param>
    /// <param name="shouldUpdateTimestamp">Should the timestamp be updated when an entity is updated by the repository (default is true).</param>
    public CosmosSingleTableOptions(string databaseId, string containerId, bool shouldUpdateTimestamp = true) : base(databaseId, containerId, shouldUpdateTimestamp)
    {
    }

    /// <summary>
    /// Gets a <see cref="PartitionKey"/> containing the entities Id.
    /// </summary>
    /// <param name="entity">The <see cref="CosmosTableData{T}"/> to retrieve the partition key from.</param>
    /// <param name="partitionKey">A <see cref="PartitionKey"/> containing the entities Id.</param>
    /// <returns>The original id of the entity.</returns>

    public override string GetPartitionKey(TEntity entity, out PartitionKey partitionKey)
    {
        partitionKey = new PartitionKey(entity.Id);

        return entity.Id;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="partitionKey"></param>
    /// <returns></returns>
    public override string ParsePartitionKey(string id, out PartitionKey partitionKey)
    {
        partitionKey = new PartitionKey(id);
        return id;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override Expression<Func<TEntity, bool>> QueryablePredicate() => (_) => true;
}
