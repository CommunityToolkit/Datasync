// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Http;
using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Server.InMemory;

/// <summary>
/// An implementation of the repository pattern used to store entities for the Datasync Toolkit
/// with an in-memory data store.
/// </summary>
/// <remarks>
/// This repository is used during testing of the Datasync Toolkit and is not recommended for
/// production use.</remarks>
/// <typeparam name="TEntity">The type of entity being stored in the repository.</typeparam>
public class InMemoryRepository<TEntity> : IRepository<TEntity> where TEntity : InMemoryTableData
{
    private readonly ConcurrentDictionary<string, TEntity> _entities = new();

    /// <summary>
    /// Creates a new empty <see cref="InMemoryRepository{TEntity}"/> repository instance.
    /// </summary>
    public InMemoryRepository()
    {
    }

    /// <summary>
    /// Creates a new populated <see cref="InMemoryRepository{TEntity}"/> repository instance.
    /// </summary>
    /// <param name="entities">A set of entities to be stored in the repository.</param>
    public InMemoryRepository(IEnumerable<TEntity> entities)
    {
        foreach (TEntity entity in entities)
        {
            entity.Id ??= Guid.NewGuid().ToString();
            StoreEntity(entity);
        }
    }

    #region Internal properties and methods for testing.
    /// <summary>
    /// If set, the repository will throw this exception when any method is called.
    /// </summary>
    internal Exception? ThrowException { get; set; }

    /// <summary>
    /// Used in tests to get the raw entity from the repository.
    /// </summary>
    /// <param name="id">The globally unique ID for the entity to retrieve.</param>
    /// <returns>The stored entity, or <c>null</c> if the entity does not exist.</returns>
    internal TEntity? GetEntity(string id) => this._entities.TryGetValue(id, out TEntity? entity) ? entity : null;

    /// <summary>
    /// Used in tests to get all the entities from the repository.
    /// </summary>
    /// <returns>A list of entities in the repository.</returns>
    internal List<TEntity> GetEntities() => [.. this._entities.Values];

    /// <summary>
    /// Clears the repository of all entities so that tests are predicatable.
    /// </summary>
    internal void Clear() => this._entities.Clear();
    #endregion

    /// <summary>
    /// Creates a clone of the provided entity.
    /// </summary>
    /// <remarks>
    /// This uses the DatasyncServiceOptions.JsonSerializerOptions to serialize and deserialize the entity.
    /// </remarks>
    /// <param name="entity">The entity to clone.</param>
    /// <returns>A copy of the entity.</returns>
    protected static TEntity Disconnect(TEntity entity)
        => entity.Clone();

    /// <summary>
    /// Removes an entity from the repository store.
    /// </summary>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <exception cref="RepositoryException">Thrown if the entity cannot be removed.</exception>
    internal void RemoveEntity(string id)
    {
        if (!this._entities.TryRemove(id, out _))
        {
            throw new RepositoryException("Failed to remove entity from the repository");
        }
    }

    /// <summary>
    /// Updates the entity metadata and stores the new entity into the repository.
    /// </summary>
    /// <param name="entity">The entity to store in the repository.</param>
    internal void StoreEntity(TEntity entity)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.Version = Guid.NewGuid().ToByteArray();
        this._entities[entity.Id] = Disconnect(entity);
    }

    /// <summary>
    /// Throws an exception if requested by the <see cref="ThrowException"/> property.
    /// </summary>
    protected void ThrowExceptionIfSet()
    {
        if (ThrowException != null)
        {
            throw ThrowException;
        }
    }

    #region IRepository<TEntity> implementation
    /// <inheritdoc />
    public virtual ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default)
    {
        ThrowExceptionIfSet();
        return ValueTask.FromResult(this._entities.Values.AsQueryable());
    }

    /// <inheritdoc />
    public virtual ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ThrowExceptionIfSet();
        if (string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = Guid.NewGuid().ToString();
        }

        if (this._entities.TryGetValue(entity.Id, out TEntity? storedEntity))
        {
            throw new HttpException(HttpStatusCodes.Status409Conflict) { Payload = Disconnect(storedEntity) };
        }

        StoreEntity(entity);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public virtual ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        ThrowExceptionIfSet();
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException(HttpStatusCodes.Status400BadRequest);
        }

        if (!this._entities.TryGetValue(id, out TEntity? storedEntity))
        {
            throw new HttpException(HttpStatusCodes.Status404NotFound);
        }

        if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
        {
            throw new HttpException(HttpStatusCodes.Status412PreconditionFailed) { Payload = Disconnect(storedEntity) };
        }

        RemoveEntity(id);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public virtual ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowExceptionIfSet();
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException(HttpStatusCodes.Status400BadRequest);
        }

        if (!this._entities.TryGetValue(id, out TEntity? storedEntity))
        {
            throw new HttpException(HttpStatusCodes.Status404NotFound);
        }

        return ValueTask.FromResult(Disconnect(storedEntity));
    }

    /// <inheritdoc />
    public virtual ValueTask ReplaceAsync(TEntity entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        ThrowExceptionIfSet();
        if (string.IsNullOrEmpty(entity.Id))
        {
            throw new HttpException(HttpStatusCodes.Status400BadRequest);
        }

        if (!this._entities.TryGetValue(entity.Id, out TEntity? storedEntity))
        {
            throw new HttpException(HttpStatusCodes.Status404NotFound);
        }

        if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
        {
            throw new HttpException(HttpStatusCodes.Status412PreconditionFailed) { Payload = Disconnect(storedEntity) };
        }

        StoreEntity(entity);
        return ValueTask.CompletedTask;
    }
    #endregion
}
