// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The manager for the datasync service operations queue.
/// </summary>
/// <param name="context">The database context.</param>
internal class OperationsQueueManager(OfflineDbContext context) : IDisposable
{
    /// <summary>
    /// A checker for the disposed state of this context.
    /// </summary>
    internal bool _disposedValue;

    /// <summary>
    /// The internal entity map for mapping entities that have been determined to be valid synchronization targets.
    /// </summary>
    internal Dictionary<string, Type> DatasyncEntityMap { get; } = [];

    /// <summary>
    /// The JSON Serializer Options to use in serializing and deserializing content.
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; } = DatasyncSerializer.JsonSerializerOptions;

    /// <summary>
    /// Retrieves the existing operation that matches an operation for the provided entity.
    /// </summary>
    /// <param name="entity">The entity being processed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The operation entity or null if one does not exist.</returns>
    /// <exception cref="DatasyncException">Thrown if the entity ID of the provided entity is invalid.</exception>
    internal async ValueTask<DatasyncOperation?> GetExistingOperationAsync(object entity, CancellationToken cancellationToken = default)
    {
        Type entityType = entity.GetType();
        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity, entityType);
        if (!EntityResolver.EntityIdIsValid(metadata.Id))
        {
            throw new DatasyncException($"Entity ID for type {entityType.FullName} is invalid.");
        }

        DatasyncOperation? existingOperation = await context.DatasyncOperationsQueue
            .SingleOrDefaultAsync(x => x.EntityType == entityType.FullName && x.ItemId == metadata.Id, cancellationToken).ConfigureAwait(false);
        return existingOperation;
    }

    /// <summary>
    /// Initializes the value of the <see cref="DatasyncEntityMap"/> - this provides the mapping 
    /// of entity name to type, which is required for operating the operations queue.
    /// </summary>
    /// <remarks>
    /// An entity is "synchronization ready" if:
    /// 
    /// * It is a property on this context
    /// * The property is public and a <see cref="DbSet{TEntity}"/>.
    /// * The property does not have a <see cref="DoNotSynchronizeAttribute"/> specified.
    /// * The entity type is defined in the model.
    /// * The entity type has an Id, UpdatedAt, and Version property (according to the <see cref="EntityResolver"/>).
    /// </remarks>
    internal void InitializeDatasyncEntityMap()
    {
        if (DatasyncEntityMap.Count > 0)
        {
            // Fast return if the entity map has already been primed.
            return;
        }

        Type[] modelEntities = context.Model.GetEntityTypes().Select(m => m.ClrType).ToArray();
        Type[] synchronizableEntities = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsSynchronizationEntity)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Where(m => modelEntities.Contains(m))
            .ToArray();
        foreach (Type entityType in synchronizableEntities)
        {
            DatasyncException.ThrowIfNullOrEmpty(entityType.FullName, $"Offline entity {entityType.Name} must be a valid reference type.");
            EntityResolver.EntityPropertyInfo propInfo = EntityResolver.GetEntityPropertyInfo(entityType);
            DatasyncException.ThrowIfNull(propInfo.UpdatedAtPropertyInfo, $"Offline entity {entityType.Name} does not have an UpdatedAt property.");
            DatasyncException.ThrowIfNull(propInfo.VersionPropertyInfo, $"Offline entity {entityType.Name} does not have a Version property.");
            DatasyncEntityMap.Add(entityType.FullName!, entityType);
        }
    }

    /// <summary>
    /// Determines if the provided property is a synchronizable property.  
    /// </summary>
    /// <remarks>
    /// An entity is "synchronization ready" if:
    /// 
    /// * It is a property on this context
    /// * The property is public and a <see cref="DbSet{TEntity}"/>.
    /// * The property does not have a <see cref="DoNotSynchronizeAttribute"/> specified.
    /// </remarks>
    /// <param name="property">The <see cref="PropertyInfo"/> for the property to check.</param>
    /// <returns><c>true</c> if the property is synchronizable; <c>false</c> otherwise.</returns>
    internal bool IsSynchronizationEntity(PropertyInfo property)
    {
        if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        {
            if (property.GetCustomAttribute<DoNotSynchronizeAttribute>() == null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// a helper method for returning the empty string instead of null when there is a nullable string.
    /// </summary>
    /// <param name="nullableString">The nullable string</param>
    /// <returns>The non-nullable string.</returns>
    internal static string NullAsEmpty(string? nullableString)
        => nullableString ?? string.Empty;

    /// <summary>
    /// Converts the EntityState to an OperationKind.
    /// </summary>
    /// <param name="entityState">The <see cref="EntityState"/> to convert.</param>
    /// <returns>The equivalent <see cref="OperationKind"/>.</returns>
    /// <exception cref="InvalidOperationException">If the entity state is not valid as an operation.</exception>
    internal static OperationKind ToOperationKind(EntityState entityState)
        => entityState switch
        {
            EntityState.Deleted => OperationKind.Delete,
            EntityState.Modified => OperationKind.Replace,
            EntityState.Added => OperationKind.Add,
            _ => throw new InvalidOperationException($"Invalid ChangeTracker EntryEntity state = {entityState}"),
        };

    /// <summary>
    /// Updates an existing operation according to a ruleset for the new operation.
    /// </summary>
    /// <param name="existingOperation">The existing operation.</param>
    /// <param name="updatedOperation">The updated operation.</param>
    internal void UpdateExistingOperation(DatasyncOperation existingOperation, DatasyncOperation updatedOperation)
    {
        // Add followed by Delete
        if (existingOperation.Kind is OperationKind.Add && updatedOperation.Kind is OperationKind.Delete)
        {
            _ = context.DatasyncOperationsQueue.Remove(existingOperation);
            return;
        }

        // Add followed by Replace
        if (existingOperation.Kind is OperationKind.Add && updatedOperation.Kind is OperationKind.Replace)
        {
            existingOperation.Item = updatedOperation.Item;
            UpdateOperation(existingOperation);
            return;
        }

        // Delete followed by Add
        if (existingOperation.Kind is OperationKind.Delete && updatedOperation.Kind is OperationKind.Add)
        {
            existingOperation.Kind = OperationKind.Replace;
            existingOperation.Item = updatedOperation.Item;
            UpdateOperation(existingOperation);
            return;
        }

        // Replace followed by Delete
        if (existingOperation.Kind is OperationKind.Replace && updatedOperation.Kind is OperationKind.Delete)
        {
            existingOperation.Kind = OperationKind.Delete;
            UpdateOperation(existingOperation);
            return;
        }

        // Replace followed by Replace
        if (existingOperation.Kind is OperationKind.Replace && updatedOperation.Kind is OperationKind.Replace)
        {
            existingOperation.Item = updatedOperation.Item;
            UpdateOperation(existingOperation);
            return;
        }

        throw new DatasyncQueueException(existingOperation, updatedOperation);
    }

    /// <summary>
    /// Updates the provided operation and stores in the context.
    /// </summary>
    /// <param name="operation">The operation to update.</param>
    internal void UpdateOperation(DatasyncOperation operation)
    {
        operation.State = OperationState.Pending;
        operation.Version++;
        _ = context.DatasyncOperationsQueue.Update(operation);
    }

    /// <summary>
    /// Iterates through each of the changes in the dataset to add each change to the operations queue.
    /// </summary>
    /// <remarks>
    /// This method calls the async version in a thread pool.  Prefer the async version to avoid deadlock issues.
    /// </remarks>
    public void UpdateOperationsQueue()
        => Task.Run(() => UpdateOperationsQueueAsync()).GetAwaiter().GetResult();

    /// <summary>
    /// Iterates through each of the changes in the dataset to add each change to the operations queue (asynchronously).
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that runs asynchronously.</returns>
    public async Task UpdateOperationsQueueAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        InitializeDatasyncEntityMap();

        if (context.ChangeTracker.AutoDetectChangesEnabled)
        {
            context.ChangeTracker.DetectChanges();
        }

        // Get the list of relevant changes from the change tracker:
        IEnumerable<EntityEntry> entitiesInScope = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => DatasyncEntityMap.ContainsKey(NullAsEmpty(e.Entity.GetType().FullName)));

        // Get the current sequence ID.
        long sequenceId = await context.DatasyncOperationsQueue.MaxAsync(x => x.Sequence, cancellationToken).ConfigureAwait(false);

        // Rest of the tracker here.
        foreach (EntityEntry entry in entitiesInScope)
        {
            Type entityType = entry.Entity.GetType();
            EntityMetadata metadata = EntityResolver.GetEntityMetadata(entry.Entity, entityType);
            if (!EntityResolver.EntityIdIsValid(metadata.Id))
            {
                throw new DatasyncException($"Entity ID for type {entityType.FullName} is invalid.");
            }

            DatasyncOperation operation = new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Kind = ToOperationKind(entry.State),
                State = OperationState.Pending,
                EntityType = NullAsEmpty(entityType.FullName),
                ItemId = metadata.Id!,
                Item = JsonSerializer.Serialize(entry.Entity, entityType, JsonSerializerOptions),
                Sequence = sequenceId,
                Version = 0
            };

            DatasyncOperation? existingOperation = await GetExistingOperationAsync(entry.Entity, cancellationToken).ConfigureAwait(false);
            if (existingOperation is null)
            {
                operation.Sequence = sequenceId++;
                _ = await context.DatasyncOperationsQueue.AddAsync(operation, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                UpdateExistingOperation(existingOperation, operation);
            }
        }
    }

    #region IDisposable
    /// <summary>
    /// Ensure that the context has not been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the context has been disposed already.</exception>
    [DebuggerStepThrough]
    internal void CheckDisposed()
    {
        if (this._disposedValue)
        {
            throw new ObjectDisposedException(GetType().ShortDisplayName(), CoreStrings.ContextDisposed);
        }
    }

    /// <summary>
    ///     Releases the allocated resources for this context.
    /// </summary>
    /// <param name="disposing"></param>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>
    ///     for more information and examples.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (!this._disposedValue)
        {
            if (disposing)
            {
                // Remove any managed content here.
            }

            this._disposedValue = true;
        }
    }

    /// <summary>
    ///     Releases the allocated resources for this context.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>
    ///     for more information and examples.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
