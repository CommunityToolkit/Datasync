// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.WinUI3.Database;

namespace TodoApp.WinUI3.Services;

/// <summary>
/// The implementation of a CRUD interface to the local data store.
/// </summary>
/// <typeparam name="T">The type of entity being stored in the data store.</typeparam>
/// <param name="context">The database context.</param>
public abstract class LocalDataService<T> : IDataService<T> where T : OfflineClientEntity
{
    public LocalDataService(AppDbContext context)
    {
        _ = context.Database.EnsureCreated();
        Context = context;
        Dataset = context.Set<T>();
    }

    /// <summary>
    /// The database context.
    /// </summary>
    internal AppDbContext Context { get; }

    /// <summary>
    /// The data store representation for this entity.
    /// </summary>
    internal DbSet<T> Dataset { get; }

    /// <summary>
    /// Create a new entity within the data store.
    /// </summary>
    /// <param name="entity">The entity to be added.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The entity as stored in the data store.</returns>
    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = Guid.NewGuid().ToString("N");
        }

        EntityEntry<T> result = await Dataset.AddAsync(entity, cancellationToken);
        _ = await Context.SaveChangesAsync(cancellationToken);
        return result.Entity;
    }

    /// <summary>
    /// Removes an entity from the data store.
    /// </summary>
    /// <param name="entity">The entity to be removed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns><c>true</c> if the entity no longer exists in the data store; <c>false</c> otherwise.</returns>
    public async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        EntityEntry<T> result = Dataset.Remove(entity);
        _ = await Context.SaveChangesAsync(cancellationToken);
        return result == null || result.State == EntityState.Deleted;
    }

    /// <summary>
    /// Retrieves all the entities from the data store.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An enumerable of all the entities in the data store.</returns>
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Dataset.ToListAsync(cancellationToken);

    /// <summary>
    /// Retrieves a single entity from the data store.
    /// </summary>
    /// <param name="id">The globally unique ID for the entity to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The entity as stored in the data store.</returns>
    public async Task<T> GetAsync(string id, CancellationToken cancellationToken = default)
        => await Dataset.FindAsync([id], cancellationToken);

    /// <summary>
    /// Updates the entity within the data store with new information.
    /// </summary>
    /// <param name="entity">The updated entity.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The entity as stored in the data store.</returns>
    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        EntityEntry<T> result = Dataset.Update(entity);
        _ = await Context.SaveChangesAsync(cancellationToken);
        return result.Entity;
    }
}
