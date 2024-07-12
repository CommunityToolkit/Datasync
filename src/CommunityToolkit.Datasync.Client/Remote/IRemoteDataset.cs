// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Models;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// Definition of the operations that can be performed against a remote dataset.  This interface
/// covers the read portion of the API.  Write operations are covered with the <see cref="IRemoteDataset{T}"/>
/// interface.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IReadOnlyRemoteDataset<T> where T : notnull
{
    /// <summary>
    /// Count the number of items that would be returned by the provided query, without returning all the values.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="options">The options to use with this operation</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the number of items that will be in the result set when the query finishes.</returns>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    ValueTask<long> CountAsync(string query, RemoteOperationOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve an item from the remote dataset.
    /// </summary>
    /// <param name="id">The ID of the item to retrieve.</param>
    /// <param name="options">The options to use with this operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the item when complete.</returns>
    /// <exception cref="ArgumentException">If the globally unique ID is not a valid ID.</exception>
    /// <exception cref="EntityNotFoundException">Thrown if the entity does not exist on the server.</exception>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    ValueTask<T> GetAsync(string id, RemoteOperationOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single page of items produced as a result of a query against the server.
    /// </summary>
    /// <param name="pathAndQuery">The path and query for the page of items.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns a page of items when complete.</returns>
    /// <exception cref="UriFormatException">Thrown if the <paramref name="pathAndQuery"/> cannot be used to construct a valid URI.</exception>
    ValueTask<Page<T>> GetPageAsync(string pathAndQuery, CancellationToken cancellationToken = default);
}

/// <summary>
/// Definition of the operations that can be performed against a remote dataset.  This interface covers the write portion 
/// of the API.  Read operations are covered with the <see cref="IReadOnlyRemoteDataset{T}"/> interface.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRemoteDataset<T> : IReadOnlyRemoteDataset<T> where T : notnull
{
    /// <summary>
    /// Adds a new entity to the remote dataset.
    /// </summary>
    /// <param name="entity">The instance to add to the remote dataset.</param>
    /// <param name="options">The options to use with this operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the inserted data when complete.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity provided is not valid (client-side evaluation).</exception>
    /// <exception cref="ConflictException{T}">Thrown if the entity already exists on the server.</exception>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    Task<T> AddAsync(T entity, RemoteOperationOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an existing entity from the remote dataset.
    /// </summary>
    /// <param name="id">The globally unique ID of the entity to be removed.</param>
    /// <param name="options">The options to use with this operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns when the operation is complete.</returns>
    /// <exception cref="ArgumentException">Thrown if the globally unique ID provided is not valid.</exception>
    /// <exception cref="EntityNotFoundException">Thrown if the entity does not exist on the server.</exception>
    /// <exception cref="ConflictException{T}">Thrown if there is a version mismatch on the server.</exception>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    Task RemoveAsync(string id, RemoteOperationOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing entity in the remote dataset.
    /// </summary>
    /// <param name="entity">The entity to replace in the remote dataset.</param>
    /// <param name="options">The options to use with this operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the replaced data when complete.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity provided is not valid.</exception>
    /// <exception cref="ConflictException{T}">Thrown if there is a version mismatch on the server.</exception>
    /// <exception cref="EntityNotFoundException">Thrown if the entity does not exist on the server.</exception>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    Task<T> ReplaceAsync(T entity, RemoteOperationOptions options, CancellationToken cancellationToken = default);
}