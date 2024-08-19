// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Internal;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System.Reflection;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline;

public partial class OfflineDbContext
{
    /// <summary>
    /// The internal details of the <see cref="OfflineDbContext"/>.
    /// </summary>
    internal class SyncContext
    {
        /// <summary>
        /// True if the context has been disposed.
        /// </summary>
        internal bool _disposedValue;

        /// <summary>
        /// The map of valid entities that can be synchronized to the service.
        /// </summary>
        internal Dictionary<string, Type> _entityMap;

        /// <summary>
        /// The offline options for the service.
        /// </summary>
        internal Lazy<OfflineOptions> _offlineOptions;

        /// <summary>
        /// The underlying <see cref="OfflineDbContext"/> to use within this internal API.
        /// </summary>
        internal OfflineDbContext _context;

        /// <summary>
        /// Initializes the internal API.
        /// </summary>
        /// <param name="context"></param>
        internal SyncContext(OfflineDbContext context)
        {
            this._context = context;
            this._entityMap = GetEntityMap(context);
            this._offlineOptions = new(() => GetOfflineOptions(context, [.. this._entityMap.Values]));
        }

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
            ArgumentNullException.ThrowIfNull(context, nameof(context));

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
        /// Builds the offline options for a datasync operation.
        /// </summary>
        /// <param name="context">The <see cref="OfflineDbContext"/> for the datasync entities.</param>
        /// <param name="entityTypes">The list of entity types that are synchronizable.</param>
        /// <returns>The offline options for the datasync operation.</returns>
        internal static OfflineOptions GetOfflineOptions(OfflineDbContext context, IList<Type> entityTypes)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            DatasyncOfflineOptionsBuilder builder = new(entityTypes);
            context.OnDatasyncInitialization(builder);
            return builder.Build();
        }

        /// <summary>
        /// Retrieves the list of queued operations for the service.
        /// </summary>
        /// <param name="entityTypeNames">The list of entity types that are in scope.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns>The list of queued operations.</returns>
        internal Task<List<DatasyncOperation>> GetQueuedOperationsAsync(List<string> entityTypeNames, CancellationToken cancellationToken = default)
            => this._context.DatasyncOperationsQueue.Where(x => entityTypeNames.Contains(x.EntityType) && x.State != OperationState.Completed).ToListAsync(cancellationToken);

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
            => allowedTypes.Where(x => this._entityMap.ContainsValue(x));

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
        ///     Pushes entities from the selected entity types to the remote service.
        /// </summary>
        /// <param name="entityTypes">The entity types in scope for this push operation.</param>
        /// <param name="pushOptions">The options to use for this push operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns>The results of the push operation.</returns>
        internal async Task<PushOperationResult> PushAsync(IEnumerable<Type> entityTypes, PushOptions pushOptions, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entityTypes, nameof(entityTypes));
            ArgumentValidationException.ThrowIfNotValid(pushOptions, nameof(pushOptions));
            List<string> entityTypeNames = GetSynchronizableEntityTypes(entityTypes).Select(x => x.FullName!).ToList();
            if (entityTypeNames.Count == 0)
            {
                return new PushOperationResult();
            }

            if (pushOptions.AutoSave)
            {
                _ = await this._context.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken).ConfigureAwait(false);
            }

            using IDisposable syncLock = await LockManager.AcquireSynchronizationLockAsync(cancellationToken).ConfigureAwait(false);
            return await PushAsyncInner(entityTypeNames, pushOptions, cancellationToken).ConfigureAwait(false);
        }
        
        /// <summary>
        /// This is the same as <see cref="PushAsync(IEnumerable{Type}, PushOptions, CancellationToken)"/>, but without the lock.  This is so that it
        /// can be re-used as part of the PullAsync operation which also requires a synchronization lock.
        /// </summary>
        /// <param name="entityTypeNames">The list of entity type names that are being pushed.</param>
        /// <param name="pushOptions">The push options.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns>The push result.</returns>
        internal async Task<PushOperationResult> PushAsyncInner(List<string> entityTypeNames, PushOptions pushOptions, CancellationToken cancellationToken = default)
        {
            PushOperationResult result = new();
            List<DatasyncOperation> queuedOperations = await GetQueuedOperationsAsync(entityTypeNames, cancellationToken).ConfigureAwait(false);
            if (queuedOperations.Count == 0)
            {
                return result;
            }

            QueueHandler<DatasyncOperation> queueHandler = new(pushOptions.ParallelOperations, async (operation) =>
            {
                ServiceResponse? response = await PushOperationAsync(operation, cancellationToken).ConfigureAwait(false);
                result.AddOperationResult(operation.Id, response);
            });
            queueHandler.EnqueueRange(queuedOperations);
            await queueHandler.WhenComplete();

            if (pushOptions.AutoSave)
            {
                _ = await this._context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false, cancellationToken).ConfigureAwait(false);
            }

            return result;
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
            HttpClient client = this._offlineOptions.Value.GetClient(entityType);
            Uri endpoint  = this._offlineOptions.Value.GetEndpoint(entityType);

            ExecutableOperation op = await ExecutableOperation.CreateAsync(operation, cancellationToken).ConfigureAwait(false);
            ServiceResponse response = await op.ExecuteAsync(client, endpoint, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessful)
            {
                operation.LastAttempt = DateTimeOffset.UtcNow;
                operation.HttpStatusCode = response.StatusCode;
                operation.State = OperationState.Failed;
                _ = this._context.Update(operation);
                return response;
            }

            if (operation.Kind != OperationKind.Delete)
            {
                object? newValue = JsonSerializer.Deserialize(response.ContentStream, entityType, DatasyncSerializer.JsonSerializerOptions);
                object? oldValue = await this._context.FindAsync(entityType, [operation.ItemId], cancellationToken).ConfigureAwait(false);
                ReplaceDatabaseValue(oldValue, newValue);
            }

            _ = this._context.DatasyncOperationsQueue.Remove(operation);
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

            EntityEntry tracker = this._context.Entry(oldValue);
            tracker.CurrentValues.SetValues(newValue);
        }

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
            using IDisposable syncLock = await LockManager.AcquireLockAsync(LockManager.synchronizationLockName, cancellationToken).ConfigureAwait(false);

            if (this._context.ChangeTracker.AutoDetectChangesEnabled)
            {
                this._context.ChangeTracker.DetectChanges();
            }

            // Get the list of relevant changes from the change tracker:
            List<EntityEntry> entitiesInScope = this._context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => this._entityMap.ContainsKey(e.Entity.GetType().FullName.AsNullableEmptyString()))
                .ToList();

            // Get the current sequence ID.  Note that ORDERBY/TOP is generally faster than aggregate functions in databases.
            // The .FirstOrDefaultAsync() returns default(long) which is 0L.
            long sequenceId = await this._context.DatasyncOperationsQueue
                .OrderByDescending(x => x.Sequence)
                .Select(x => x.Sequence)
                .FirstOrDefaultAsync(cancellationToken);

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
                    EntityType = entityType.FullName.AsNullableEmptyString(),
                    ItemId = metadata.Id!,
                    EntityVersion = metadata.Version ?? string.Empty,
                    Item = JsonSerializer.Serialize(entry.Entity, entityType, this._context.JsonSerializerOptions),
                    Sequence = sequenceId,
                    Version = 0
                };

                DatasyncOperation? existingOperation = await GetExistingOperationAsync(entry.Entity, cancellationToken).ConfigureAwait(false);
                if (existingOperation is null)
                {
                    operation.Sequence = sequenceId++;
                    _ = await this._context.DatasyncOperationsQueue.AddAsync(operation, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    UpdateExistingOperation(existingOperation, operation);
                }
            }
        }
    }
}
