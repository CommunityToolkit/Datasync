// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Offline.Operations;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline.OperationsQueue;

/// <summary>
/// The operations queue is a list of pending changes that have not
/// been sent to the remote service yet.
/// </summary>
internal class OperationsQueueManager : IOperationsQueueManager
{
    /// <summary>
    /// A lock object for locking against concurrent changes to the queue.
    /// </summary>
    private readonly object pushlock = new();

    /// <summary>
    /// The map of valid entities that can be synchronized to the service.
    /// </summary>
    internal OfflineDbContext _context;

    /// <summary>
    /// The map of valid entities that can be synchronized to the service.
    /// </summary>
    internal Dictionary<string, Type> _entityMap;

    /// <summary>
    /// The offline options for the service.
    /// </summary>
    internal Lazy<OfflineOptions> _offlineOptions;

    /// <summary>
    /// A reference to the internal operations queue.  This is not synchronized to
    /// the remote service.
    /// </summary>
    internal DbSet<DatasyncOperation> OperationsQueue => this._context.DatasyncOperationsQueue;

    /// <summary>
    /// A reference to the ChangeTracker for the context.
    /// </summary>
    internal ChangeTracker ChangeTracker => this._context.ChangeTracker;

    /// <summary>
    /// Creates a new <see cref="OperationsQueueManager"/> based on the context.
    /// </summary>
    /// <param name="context">The <see cref="OfflineDbContext"/> to use for storing the operations queue.</param>
    internal OperationsQueueManager(OfflineDbContext context)
    {
        this._context = context;
        this._entityMap = GetEntityMap(context);
        this._offlineOptions = new(context.BuildDatasyncOfflineOptions);
    }

    /// <summary>
    /// Retrieves the list of changes recorded in the <see cref="ChangeTracker"/> that are
    /// in scope for the operations queue.
    /// </summary>
    /// <returns>A list of <see cref="EntityEntry"/> values.</returns>
    internal List<EntityEntry> GetChangedEntitiesInScope()
        => ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => this._entityMap.ContainsKey(e.Entity.GetType().FullName.AsNullableEmptyString()))
            .ToList();

    /// <summary>
    /// Retrieves the list of synchronizable entities that are available for datasync operations.
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
    internal Dictionary<string, Type> GetEntityMap(OfflineDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Type[] modelEntities = context.Model.GetEntityTypes().Select(m => m.ClrType).ToArray();
        Type[] synchronizableEntities = context.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsSynchronizationEntity)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToArray();
        Dictionary<string, Type> entityMap = [];

        foreach (Type entityType in synchronizableEntities)
        {
            DatasyncException.ThrowIfNullOrEmpty(entityType.FullName, $"Offline entity {entityType.Name} must be a valid reference type.");
            EntityResolver.EntityPropertyInfo propInfo = EntityResolver.GetEntityPropertyInfo(entityType);
            DatasyncException.ThrowIfNull(propInfo.UpdatedAtPropertyInfo, $"Offline entity {entityType.Name} does not have an UpdatedAt property.");
            DatasyncException.ThrowIfNull(propInfo.VersionPropertyInfo, $"Offline entity {entityType.Name} does not have a Version property.");
            entityMap.Add(entityType.FullName!, entityType);
        }

        return entityMap;
    }

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

        DatasyncOperation? existingOperation = await this._context.DatasyncOperationsQueue
            .SingleOrDefaultAsync(x => x.EntityType == entityType.FullName && x.ItemId == metadata.Id, cancellationToken).ConfigureAwait(false);
        return existingOperation;
    }

    /// <summary>
    /// An asynchronous process for getting the last sequence ID used by the operations queue.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The last sequence ID in use, <c>0L</c> by default.</returns>
    internal Task<long> GetLastSequenceIdAsync(CancellationToken cancellationToken = default)
        => OperationsQueue.OrderByDescending(x => x.Sequence).Select(x => x.Sequence).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Converts an EntryEntry change from the change tracker into a <see cref="DatasyncOperation"/>
    /// </summary>
    /// <param name="entry">The entry to process.</param>
    /// <returns>The operation definition.</returns>
    internal DatasyncOperation GetOperationForChangedEntity(EntityEntry entry)
    {
        Type entityType = entry.Entity.GetType();
        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entry.Entity, entityType);
        if (!EntityResolver.EntityIdIsValid(metadata.Id))
        {
            throw new DatasyncException($"Entity ID for type {entityType.FullName} is invalid.");
        }

        return new DatasyncOperation()
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = entry.State.ToOperationKind(),
            State = OperationState.Pending,
            EntityType = entityType.FullName.AsNullableEmptyString(),
            ItemId = metadata.Id!,
            EntityVersion = metadata.Version ?? string.Empty,
            Item = JsonSerializer.Serialize(entry.Entity, entityType, this._context.JsonSerializerOptions),
            Sequence = 0,
            Version = 0
        };
    }

    /// <summary>
    /// Counts the number of operations enqueued in the operations queue for the given
    /// entity type.
    /// </summary>
    /// <param name="entityType">The entity type being processed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The number of operations in queue for the entity.</returns>
    public Task<int> CountOperationsInQueueAsync(Type entityType, CancellationToken cancellationToken = default)
        => this._context.DatasyncOperationsQueue
            .Where(x => x.EntityType == entityType.FullName! && x.State != OperationState.Completed)
            .CountAsync(cancellationToken);

    /// <summary>
    /// Retrieves the list of queued operations for the service.
    /// </summary>
    /// <param name="entityTypeNames">The list of entity types that are in scope.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The list of queued operations.</returns>
    internal Task<List<DatasyncOperation>> GetQueuedOperationsAsync(List<string> entityTypeNames, CancellationToken cancellationToken = default)
        => this._context.DatasyncOperationsQueue
            .Where(x => entityTypeNames.Contains(x.EntityType) && x.State != OperationState.Completed)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Returns the associated type for the operation queue name.
    /// </summary>
    /// <param name="fullName">The name of the type.</param>
    /// <returns>The type.</returns>
    internal Type? GetSynchronizableEntityType(string fullName)
        => this._entityMap.TryGetValue(fullName, out Type? entityType) ? entityType : null;

    /// <summary>
    /// Returns the list of types  that are "synchronizable".
    /// </summary>
    /// <returns>The list of allowed synchronizable types.</returns>
    internal IEnumerable<Type> GetSynchronizableEntityTypes()
        => this._entityMap.Values;

    /// <summary>
    /// Returns the list of types from the allowed types that are "synchronizable".
    /// </summary>
    /// <param name="allowedTypes">The list of allowed types.</param>
    /// <returns>The list of allowed synchronizable types.</returns>
    internal IEnumerable<Type> GetSynchronizableEntityTypes(IEnumerable<Type> allowedTypes)
        => allowedTypes.Where(this._entityMap.ContainsValue);

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
    /// Pushes entities from the selected entity types to the remote service.
    /// </summary>
    /// <param name="entityTypes">The entity types in scope for this push operation.</param>
    /// <param name="pushOptions">The options to use for this push operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation (asynchronously)</returns>
    internal async Task<PushResult> PushAsync(IEnumerable<Type> entityTypes, PushOptions pushOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityTypes);
        ArgumentValidationException.ThrowIfNotValid(pushOptions, nameof(pushOptions));
        PushResult pushResult = new();

        // Ensure we are actually allowed to push an entity type.
        List<string> entityTypeNames = GetSynchronizableEntityTypes(entityTypes).Select(x => x.FullName!).ToList();
        if (entityTypeNames.Count == 0)
        {
            return pushResult;
        }

        // Automatically save any changes to the context before we continue - this will update the queue.
        _ = await this._context.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken).ConfigureAwait(false);

        // Lock against other synchronization events.
        using IDisposable syncLock = await LockManager.AcquireSynchronizationLockAsync(cancellationToken).ConfigureAwait(false);

        // Determine the list of queued operations in scope.
        List<DatasyncOperation> queuedOperations = await GetQueuedOperationsAsync(entityTypeNames, cancellationToken).ConfigureAwait(false);
        if (queuedOperations.Count == 0)
        {
            return pushResult;
        }

        // Push things in parallel, according to the PushOptions
        QueueHandler<DatasyncOperation> queueHandler = new(pushOptions.ParallelOperations, async operation =>
        {
            ServiceResponse? response = await PushOperationAsync(operation, cancellationToken).ConfigureAwait(false);
            pushResult.AddOperationResult(operation, response);
        });

        // Enqueue and process all the queued operations in scope
        queueHandler.EnqueueRange(queuedOperations);
        await queueHandler.WhenComplete();

        // Save the changes, this time we don't update the queue.
        _ = await this._context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false, cancellationToken).ConfigureAwait(false);
        return pushResult;
    }

    /// <summary>
    /// Pushes a single operation in the context of a lock, updating the database at the same time.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the operation.</returns>
    internal async Task<ServiceResponse?> PushOperationAsync(DatasyncOperation operation, CancellationToken cancellationToken = default)
    {
        Type entityType = GetSynchronizableEntityType(operation.EntityType)
            ?? throw new DatasyncException($"Type '{operation.EntityType}' is not a synchronizable type.");

        EntityDatasyncOptions options = this._offlineOptions.Value.GetOptions(entityType);
        ExecutableOperation op = await ExecutableOperation.CreateAsync(operation, cancellationToken).ConfigureAwait(false);
        ServiceResponse response = await op.ExecuteAsync(options, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessful)
        {
            lock (this.pushlock)
            {
                operation.LastAttempt = DateTimeOffset.UtcNow;
                operation.HttpStatusCode = response.StatusCode;
                operation.State = OperationState.Failed;
                _ = this._context.Update(operation);
            }

            return response;
        }

        // If the operation is a success, then the content may need to be updated.
        if (operation.Kind != OperationKind.Delete)
        {
            object? newValue = JsonSerializer.Deserialize(response.ContentStream, entityType, DatasyncSerializer.JsonSerializerOptions);
            object? oldValue = await this._context.FindAsync(entityType, [operation.ItemId], cancellationToken).ConfigureAwait(false);
            ReplaceDatabaseValue(oldValue, newValue);
        }

        lock (this.pushlock)
        {
            _ = this._context.DatasyncOperationsQueue.Remove(operation);
        }

        return null;
    }

    /// <summary>
    /// Internal helper - replaces an old value of an entity in the database with a new value.
    /// </summary>
    /// <param name="oldValue">The old value.</param>
    /// <param name="newValue">The new value.</param>
    internal void ReplaceDatabaseValue(object? oldValue, object? newValue)
    {
        if (oldValue is null || newValue is null)
        {
            throw new DatasyncException("Internal Datasync Error: invalid values for replacement.");
        }

        lock (this.pushlock)
        {
            EntityEntry tracker = this._context.Entry(oldValue);
            tracker.CurrentValues.SetValues(newValue);
        }
    }

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
            _ = this._context.DatasyncOperationsQueue.Remove(existingOperation);
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
        _ = this._context.DatasyncOperationsQueue.Update(operation);
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
        this._context.CheckDisposed();
        using IDisposable syncLock = await LockManager.AcquireSynchronizationLockAsync(cancellationToken).ConfigureAwait(false);

        long sequenceId = await GetLastSequenceIdAsync(cancellationToken).ConfigureAwait(false);
        List<EntityEntry> entitiesInScope = GetChangedEntitiesInScope();
        foreach (EntityEntry entry in entitiesInScope)
        {
            DatasyncOperation newOperation = GetOperationForChangedEntity(entry);
            DatasyncOperation? existingOperation = await GetExistingOperationAsync(entry.Entity, cancellationToken).ConfigureAwait(false);
            if (existingOperation is null)
            {
                newOperation.Sequence = Interlocked.Increment(ref sequenceId);
                _ = await OperationsQueue.AddAsync(newOperation, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                UpdateExistingOperation(existingOperation, newOperation);
            }
        }
    }
}
