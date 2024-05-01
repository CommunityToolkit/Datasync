// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
///  Definition of the read operations for a remote dataset.
/// </summary>
/// <typeparam name="T">The type of entity stored in the remote dataset.</typeparam>
public interface IReadonlyRemoteDataset<T> : IODataLinqMethods<T>
{
    /// <summary>
    /// Converts the current dataset to a dataset of a different type, but using the
    /// same basic settings.
    /// </summary>
    /// <typeparam name="U">The new entity type, which should be a subset of the current entity type.</typeparam>
    /// <returns>A <see cref="IReadonlyRemoteDataset{T}"/> to access the new entity type.</returns>
    IReadonlyRemoteDataset<U> AsDataset<U>();

    /// <summary>
    /// Creates a new <see cref="IODataQuery{T}"/> object for the current dataset.
    /// </summary>
    /// <returns>A <see cref="IODataQuery{T}"/> to the current dataset.</returns>
    IODataQuery<T> AsQueryable();

    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The globally unique ID for the entity.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the entity requested when complete.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    ValueTask<T> FindAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of entities that would be returned by the query.
    /// </summary>
    /// <param name="odataQueryString">The OData querry string to send to the service.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the number of entities matching the query when complete.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    ValueTask<long> LongCountAsync(string odataQueryString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query on the remote service, allowing the enumeration of the results asynchronously.
    /// </summary>
    /// <param name="odataQueryString">The OData query string to send to the service.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> for iterating through the entities.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    IAsyncEnumerable<T> Query(string odataQueryString, CancellationToken cancellationToken = default);
}

/// <summary>
/// Definition of the read and write operations for a remote dataset.
/// </summary>
/// <typeparam name="T">The type of entity stored in the remote dataset.</typeparam>
public interface IRemoteDataset<T> : IReadonlyRemoteDataset<T>
{
    /// <summary>
    /// Adds a new entity to the remote dataset.
    /// </summary>
    /// <param name="entity">The entity to add to the remote dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the added entity (with all metadata set) when complete.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    ValueTask<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the remote dataset.
    /// </summary>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    ValueTask RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the remote dataset if it matches the provided version.
    /// </summary>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <param name="version">The required version on the service side.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    ValueTask RemoveAsync(string id, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an entity with new data if the version in the replacement data matches the version on the service.
    /// </summary>
    /// <remarks>
    /// Do not alter the system metadata (ID, Deleted, UpdatedAt, Version) in the entity.
    /// </remarks>
    /// <param name="entity">The entity with new data.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the replaced entity (with all metadata set) when complete.</returns>
    ValueTask<T> ReplaceAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an entity with new data if the version in the replacement data matches the version on the service.
    /// </summary>
    /// <remarks>
    /// Do not alter the system metadata (ID, Deleted, UpdatedAt, Version) in the entity.
    /// </remarks>
    /// <param name="entity">The entity with new data.</param>
    /// <param name="force">If true, the version is not considered in replacing the service side.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the replaced entity (with all metadata set) when complete.</returns>
    ValueTask<T> ReplaceAsync(T entity, bool force, CancellationToken cancellationToken = default);
}
