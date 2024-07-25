// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// A client class for accessing a remote datasync service.
/// </summary>
/// <typeparam name="TEntity">The type of entity being sent and received.</typeparam>
public interface IReadOnlyDatasyncClient<TEntity> where TEntity : class
{
    /// <summary>
    /// Counts the number of entities that would be returned by the given query without returning the entities.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="options">Any query options that must be considered.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="DatasyncException">Thrown if an error is encountered that is not recoverable.</exception>
    Task<DatasyncResult<long>> CountAsync(IDatasyncQueryable<TEntity> query, DatasyncQueryOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity from the remote table using the provided ID.
    /// </summary>
    /// <remarks>
    /// This method will return a result even if the entity is not found.  Any other error is considered exceptional and will be thrown.
    /// </remarks>
    /// <param name="id">The ID to retrieve.</param>
    /// <param name="options">Any options that must be considered.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="DatasyncException">Thrown if an error is encountered that is not recoverable.</exception>
    Task<DatasyncResult<TEntity>> FindAsync(string id, DatasyncGetOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <see cref="IAsyncEnumerable{T}"/> of the entities that match the query.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="options">Any query options that must be considered.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="DatasyncException">Thrown if an error is encountered that is not recoverable.</exception>
    IAsyncPageable<TEntity> Query(IDatasyncQueryable<TEntity> query, DatasyncQueryOptions options);
}

/// <summary>
/// A client class for accessing a remote datasync service.
/// </summary>
/// <typeparam name="TEntity">The type of entity being sent and received.</typeparam>
public interface IDatasyncClient<TEntity> : IReadOnlyDatasyncClient<TEntity> where TEntity : class
{
    /// <summary>
    /// Attempts to add an entity to the remote table.
    /// </summary>
    /// <param name="entity">The entity to add to the remote table.</param>
    /// <param name="options">Any options that must be considered.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="EntityExistsException">Thrown if the entity that is being added already exists on the remote table.</exception>
    /// <exception cref="DatasyncException">Thrown if an error is encountered that is not recoverable.</exception>
    Task<DatasyncResult<TEntity>> AddAsync(TEntity entity, DatasyncAddOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove an entity from the remote table.
    /// </summary>
    /// <remarks>
    /// The fact that an entity is not found is not considered an error in this method.  In this case, the DatasyncResult will
    /// return with IsSuccessful == false.
    /// </remarks>
    /// <param name="id">The ID of the entity to remove from the remote table.</param>
    /// <param name="options">Any options that must be considered.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if the entity that is being updated cannot be updated due to a conflict.</exception>
    /// <exception cref="DatasyncException">Thrown if an error is encountered that is not recoverable.</exception>
    Task<DatasyncResult> RemoveAsync(string id, DatasyncRemoveOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to update an entity in the remote table.
    /// </summary>
    /// <param name="entity">The entity to update in the remote table.</param>
    /// <param name="options">Any options that must be considered.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity that is being updated does not exist on the remote table.</exception>
    /// <exception cref="ConflictException{TEntity}">Thrown if the entity that is being updated cannot be updated due to a conflict.</exception>
    /// <exception cref="DatasyncException">Thrown if an error is encountered that is not recoverable.</exception>
    Task<DatasyncResult<TEntity>> UpdateAsync(TEntity entity, DatasyncUpdateOptions options, CancellationToken cancellationToken = default);
}
