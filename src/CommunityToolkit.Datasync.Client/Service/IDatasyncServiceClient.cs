// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;

namespace CommunityToolkit.Datasync.Client.Service;

/// <summary>
/// A read-only version of the service client that talks to a datasync service.
/// </summary>
/// <typeparam name="TEntity">The type of entity being transferred.</typeparam>
public interface IReadOnlyDatasyncServiceClient<TEntity> : IDatasyncQueryMethods<TEntity> where TEntity : class
{
    /// <summary>
    /// Creates a new <see cref="IDatasyncQueryable{TEntity}"/> for the current table.
    /// </summary>
    /// <returns>An <see cref="IDatasyncQueryable{TEntity}"/> the will return all items in the current table.</returns>
    IDatasyncQueryable<TEntity> AsQueryable();

    /// <summary>
    /// Asynchronously returns the number of entities that will be returned by the provided query.
    /// </summary>
    /// <param name="query">Tne query to execute.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    ValueTask<ServiceResponse<int>> CountAsync(IDatasyncQueryable<TEntity> query, DatasyncServiceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously returns the requested entity.
    /// </summary>
    /// <param name="id">Tne globally unique ID of the entity to be retrieved.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the requested entity.</returns>
    ValueTask<ServiceResponse<TEntity>> GetAsync(string id, DatasyncServiceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously returns a single page of results based on the provided query.
    /// </summary>
    /// <param name="query">The query to execute on the service.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns></returns>
    ValueTask<ServiceResponse<Page<TEntity>>> GetPageAsync(string query, DatasyncServiceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously returns the number of entities that will be returned by the provided query.
    /// </summary>
    /// <param name="query">Tne query to execute.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    ValueTask<ServiceResponse<long>> LongCountAsync(IDatasyncQueryable<TEntity> query, DatasyncServiceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the asynchronous list of entities matching the query.
    /// </summary>
    /// <param name="query">Tne query to execute.</param>
    /// <returns>An <see cref="IAsyncPageable{TEntity}"/> for the results that can be asynchronously iterated over.</returns>
    IAsyncPageable<TEntity> Query(IDatasyncQueryable<TEntity> query);

    /// <summary>
    /// Creates a service client for handling a different entity but using the same settings.
    /// </summary>
    /// <typeparam name="U">The type of the new entity.</typeparam>
    /// <returns>A service client for handling the new entity.</returns>
    IReadOnlyDatasyncServiceClient<U> ToServiceClient<U>() where U : class;
}

/// <summary>
/// A read/write version of the service client that talks to a datasync service.
/// </summary>
/// <typeparam name="TEntity">The type of entity being transferred</typeparam>
public interface IDatasyncServiceClient<TEntity> : IReadOnlyDatasyncServiceClient<TEntity> where TEntity : class
{
    /// <summary>
    /// Adds an entity to the remote service dataset.
    /// </summary>
    /// <param name="entity">The entity to be added to the remote service dataset.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response containing the added entity.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if the entity already exists in the remote service dataset.</exception>
    ValueTask<ServiceResponse<TEntity>> AddAsync(TEntity entity, DatasyncServiceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the remote service dataset.
    /// </summary>
    /// <param name="id">The globally unique ID for the entity to be removed from the remote service dataset.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    ValueTask<ServiceResponse> RemoveAsync(string id, DatasyncServiceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaced an entity in the remote service dataset.
    /// </summary>
    /// <param name="entity">The entity to be replaced in the remote service dataset.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response containing the replaced entity.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    ValueTask<ServiceResponse<TEntity>> ReplaceAsync(TEntity entity, DatasyncServiceOptions options, CancellationToken cancellationToken = default);
}
