// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Server.CosmosDb;
/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface ICosmosTableOptions<TEntity> where TEntity : CosmosTableData
{
    /// <summary>
    /// The name of the cosmsos database the entities are stored in
    /// </summary>
    string DatabaseId { get; }
    /// <summary>
    /// The name of the cosmos container the entities are stored in
    /// </summary>
    string ContainerId { get; }
    /// <summary>
    /// Should the timestamp be updated when an entity is updated by the repository (default is true).
    /// Should be false if there are triggers to update the timestamp on the container.
    /// </summary>
    bool ShouldUpdateTimestamp { get; }
    /// <summary>
    /// Function to attempt to parse the partition key from the entity
    /// </summary>
    /// <param name="entityId">The entity passed from the controller to the repository ID</param>
    /// <param name="id">The entity ID after the entity has been parsed</param>
    /// <param name="partitionKey">The partition key after the entity has been parsed</param>
    bool TryParsePartitionKey(string entityId, out string id, out PartitionKey partitionKey);
    /// <summary>
    /// Function to parse the partition key from the entity
    /// </summary>
    /// <param name="id">The entity id passed from the controller to the repository</param>
    /// <param name="partitionKey">The partition key after the entity has been parsed</param>
    /// <returns>The entity ID after the entity has been parsed</returns>
    string ParsePartitionKey(string id, out PartitionKey partitionKey);
    /// <summary>
    /// Get the id and partition key for the entity from the entity
    /// </summary>
    /// <param name="entity">The entity passed from the controller to the repository</param>
    /// <param name="partitionKey">The partition key after the entity has been parsed</param>
    /// <returns>The entity ID after the entity has been parsed</returns>
    string GetPartitionKey(TEntity entity, out PartitionKey partitionKey);
    /// <summary>
    /// Function to generate a new ID for the entity
    /// </summary>
    /// <value>The function to generate a new ID for the entity</value>
    Func<TEntity, string> IdGenerator { get; }
    /// <summary>
    /// Function to filter the dataset for the entity required for shared containers
    /// </summary>
    Expression<Func<TEntity, bool>> QueryablePredicate();
}
