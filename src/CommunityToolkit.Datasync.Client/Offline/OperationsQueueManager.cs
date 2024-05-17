// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
using CommunityToolkit.Datasync.Server.Abstractions.Guards;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// Operations needed to manintain the queue of operations that need to be synchronized with the remote service.
/// </summary>
/// <remarks>
/// Creates a new <see cref="OperationsQueueManager"/> where the operations are
/// stored in the given <see cref="OfflineDbContext"/>.
/// </remarks>
/// <param name="context">Tne <see cref="OfflineDbContext"/> holding the operations queue.</param>
internal class OperationsQueueManager(OfflineDbContext context) : IOperationsQueueManager
{
    /// <summary>
    /// The <see cref="OfflineDbContext"/> that is used to store the operations queue.
    /// </summary>
    internal OfflineDbContext Context = context;

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing data.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = context.JsonSerializerOptions;

    /// <summary>
    /// Creates a new Create/Insert/Add operation for the given state change.
    /// </summary>
    /// <remarks>
    /// A create operation can only be added if there is no existing operation for the same entity.  If
    /// there is an existing operation, an <see cref="InvalidOperationException"/> exception is thrown.
    /// </remarks>
    /// <param name="entry">The entry being processed.</param>
    /// <exception cref="OperationsQueueConflictException">Thrown when there is an existing operation for the same entity.</exception>
    public void AddCreateOperation(EntityEntry entry)
    {
        string entityType = entry.Entity.GetType().FullName;
        string entityId = GetEntityId(entry.OriginalValues);

        LockQueueEntity(entityType, entityId, () =>
        {
            OfflineQueueEntity queueEntity = GetExistingQueueEntity(entityType, entityId);
            if (queueEntity is not null)
            {
                throw new OperationsQueueConflictException("An operation already exists for the entity being added to the operations queue.") { QueuedOperation = queueEntity, ConflictingEntity = entry.Entity };
            }

            AddQueueEntity(EntityChangeType.Add, entityType, entityId, null, entry.CurrentValues);
        });
    }

    /// <summary>
    /// Creates a new Delete operation for the given state change.
    /// </summary>
    /// <param name="entry">The entry being processed.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity does not have an ID.</exception>
    /// <exception cref="OperationsQueueConflictException">Thrown when the entity has already been deleted.</exception>
    public void AddDeleteOperation(EntityEntry entry)
    {
        string entityType = entry.Entity.GetType().FullName;
        string entityId = GetEntityId(entry.OriginalValues);

        LockQueueEntity(entityType, entityId, () =>
        {
            OfflineQueueEntity queueEntity = GetExistingQueueEntity(entityType, entityId);
            if (queueEntity is null)
            {
                AddQueueEntity(EntityChangeType.Delete, entityType, entityId, entry.OriginalValues, null);
            }
            else if (queueEntity.ChangeType == EntityChangeType.Add)
            {
                RemoveQueueEntity(queueEntity);
            }
            else if (queueEntity.ChangeType == EntityChangeType.Delete)
            {
                throw new OperationsQueueConflictException("Attempting to delete an entity that is already queued to be deleted.") { QueuedOperation = queueEntity };
            }
            else if (queueEntity.ChangeType == EntityChangeType.Update)
            {
                queueEntity.ChangeType = EntityChangeType.Delete;
                queueEntity.ReplacementJsonEntityData = null;
                UpdateQueueEntity(queueEntity);
            }
        });
    }

    /// <summary>
    /// Creates a new Modify/Update operation for the given state change.
    /// </summary>
    /// <param name="entry">The entry being processed.</param>
    public void AddUpdateOperation(EntityEntry entry)
    {
        string entityType = entry.Entity.GetType().FullName;
        string entityId = GetEntityId(entry.OriginalValues);

        LockQueueEntity(entityType, entityId, () =>
        {
            OfflineQueueEntity queueEntity = GetExistingQueueEntity(entityType, entityId);
            if (queueEntity is null)
            {
                AddQueueEntity(EntityChangeType.Update, entityType, entityId, entry.OriginalValues, entry.CurrentValues);
            }
            else if (queueEntity.ChangeType == EntityChangeType.Delete)
            {
                throw new OperationsQueueConflictException("Attempting to update an entity that is queued to be deleted.") { QueuedOperation = queueEntity };
            }
            else
            {
                queueEntity.ReplacementJsonEntityData = Serialize(entry.CurrentValues);
                UpdateQueueEntity(queueEntity);
            }
        });
    }

    /// <summary>
    /// Returns the ID of the entity.
    /// </summary>
    /// <param name="values">The property values for the entity.</param>
    /// <returns>The entity ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity ID is not valid.</exception>
    internal static string GetEntityId(PropertyValues values)
        => ValidateEntityId(values["Id"] as string);

    /// <summary>
    /// Validates the ID of the entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <returns>The entity ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity ID is not valid.</exception>
    internal static string ValidateEntityId(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new InvalidOperationException("Attempting to enqueue an entity with an empty ID");
        }
        if (!RegexpConstants.EntityIdentity.IsMatch(entityId))
        {
            throw new InvalidOperationException("Attempting to enqueue an entity with an invalid ID");
        }
        return entityId;
    }

    /// <summary>
    /// Retrieves the existing queue entity for the given entity type and ID, if it exists.
    /// </summary>
    /// <param name="entityType">The type name of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <returns>The queued entity, or <c>null</c> if there is no queue entity for the entity type/ID</returns>
    internal OfflineQueueEntity GetExistingQueueEntity(string entityType, string entityId)
    {
        Ensure.That(entityType, nameof(entityType)).IsNotNullOrWhiteSpace();
        Ensure.That(entityId, nameof(entityId)).IsNotNullOrWhiteSpace();
        return this.Context.DatasyncOperationsQueue.Where(x => x.EntityName == entityType && x.EntityId == entityId).SingleOrDefault();
    }

    /// <summary>
    /// Wraps the provided <paramref name="action"/> in a lock for the given entity type and ID to ensure that the operations queue
    /// is not updated outside of the processing of the current operation.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="entityId">The ID of the entity type.</param>
    /// <param name="action">The action to perform within the lock.</param>
    internal static void LockQueueEntity(string entityName, string entityId, Action action)
    {
        using (QueueLockManager.AcquireLock(entityName, entityId))
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// Adds a new queue entity to the operations queue.
    /// </summary>
    /// <param name="changeType">The entity change type.</param>
    /// <param name="entityName">The name of the type being stored.</param>
    /// <param name="entityId">The globally unique ID for the entity.</param>
    /// <param name="originalValues">The original values (may be null) for the entity.</param>
    /// <param name="currentValues">The current values (may be null) for the entity.</param>
    internal void AddQueueEntity(EntityChangeType changeType, string entityName, string entityId, PropertyValues originalValues, PropertyValues currentValues)
    {
        Ensure.That(entityName, nameof(entityName)).IsNotNullOrWhiteSpace();
        Ensure.That(entityId, nameof(entityId)).IsNotNullOrWhiteSpace();
        if (changeType == EntityChangeType.Unknown)
        {
            throw new ArgumentException("Invalid change type specified for the queue entity", nameof(changeType));
        }

        OfflineQueueEntity queueEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ChangeType = changeType,
            EntityName = entityName,
            EntityId = entityId,
            OriginalJsonEntityEntity = originalValues is not null ? Serialize(originalValues) : string.Empty,
            ReplacementJsonEntityData = currentValues is not null ? Serialize(currentValues) : string.Empty
        };
        this.Context.DatasyncOperationsQueue.Add(queueEntity);
    }

    /// <summary>
    /// Removes a queue entity.
    /// </summary>
    /// <param name="queueEntity">The queue entity to be removed.</param>
    internal void RemoveQueueEntity(OfflineQueueEntity queueEntity)
    {
        Ensure.That(queueEntity, nameof(queueEntity)).IsNotNull();
        this.Context.DatasyncOperationsQueue.Remove(queueEntity);
    }

    /// <summary>
    /// Updates a queue entity with new information.
    /// </summary>
    /// <param name="queueEntity">The updated queue entity.</param>
    internal void UpdateQueueEntity(OfflineQueueEntity queueEntity)
    {
        Ensure.That(queueEntity, nameof(queueEntity)).IsNotNull();
        this.Context.DatasyncOperationsQueue.Update(queueEntity);
    }

    /// <summary>
    /// Helper method to put the serialization of the property values into a single place.
    /// </summary>
    /// <param name="properties">The <see cref="PropertyValues"/> object to serialize.</param>
    /// <returns>The serialized object.</returns>
    internal string Serialize(PropertyValues properties) => JsonSerializer.Serialize(properties.ToObject(), JsonSerializerOptions);
}
