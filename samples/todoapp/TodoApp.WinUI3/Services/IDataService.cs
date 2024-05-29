// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TodoApp.WinUI3.Services;

/// <summary>
/// A standard interface for CRUD operations against a data store.
/// </summary>
/// <typeparam name="T">The type of entity being used.</typeparam>
public interface IDataService<T> where T : OfflineClientEntity
{
    /// <summary>
    /// Create a new entity within the data store.
    /// </summary>
    /// <param name="entity">The entity to be added.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The entity as stored in the data store.</returns>
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the data store.
    /// </summary>
    /// <param name="entity">The entity to be removed.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns><c>true</c> if the entity no longer exists in the data store; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all the entities from the data store.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An enumerable of all the entities in the data store.</returns>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single entity from the data store.
    /// </summary>
    /// <param name="id">The globally unique ID for the entity to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The entity as stored in the data store.</returns>
    Task<T> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the entity within the data store with new information.
    /// </summary>
    /// <param name="entity">The updated entity.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The entity as stored in the data store.</returns>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
}
