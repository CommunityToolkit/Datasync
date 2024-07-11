// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Remote;
using CommunityToolkit.Datasync.Common;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of extension methods to make working with the <see cref="IRemoteDataset{T}"/>
/// easier.
/// </summary>
public static class IRemoteDatasetExtensions
{
    /// <summary>
    /// Adds a new entity to the remote dataset.
    /// </summary>
    /// <param name="dataset">The dataset to use for the operation.</param>
    /// <param name="entity">The entity to add to the remote dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the inserted data when complete.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity provided is not valid (client-side evaluation).</exception>
    /// <exception cref="ConflictException{T}">Thrown if the entity already exists on the server.</exception>
    /// <exception cref="DatasyncHttpException">Thrown if the response from the server does not indicate success.</exception>
    public static Task<T> AddAsync<T>(this IRemoteDataset<T> dataset, T entity, CancellationToken cancellationToken = default) where T : notnull
        => dataset.AddAsync(entity, new RemoteOperationOptions(), cancellationToken);

    /// <summary>
    /// Count the number of items that would be returned by the provided query, without returning all the values.
    /// </summary>
    /// <param name="dataset">The dataset to use for the operation.</param>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the number of items that will be in the result set when the query finishes.</returns>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    public static ValueTask<long> CountAsync<T>(this IReadOnlyRemoteDataset<T> dataset, string query, CancellationToken cancellationToken = default) where T : notnull
        => dataset.CountAsync(query, new RemoteOperationOptions(), cancellationToken);

    /// <summary>
    /// Count the number of items that would be returned without returning all the values.
    /// </summary>
    /// <param name="dataset">The dataset to use for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the number of items that will be in the result set when the query finishes.</returns>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    public static ValueTask<long> CountAsync<T>(this IReadOnlyRemoteDataset<T> dataset, CancellationToken cancellationToken = default) where T : notnull
        => dataset.CountAsync(string.Empty, new RemoteOperationOptions(), cancellationToken);

    /// <summary>
    /// Retrieve an item from the remote dataset.
    /// </summary>
    /// <param name="dataset">The dataset to use for the operation.</param>
    /// <param name="id">The ID of the item to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the item when complete.</returns>
    /// <exception cref="ArgumentException">If the globally unique ID is not a valid ID.</exception>
    /// <exception cref="EntityNotFoundException">Thrown if the entity does not exist on the server.</exception>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    public static ValueTask<T> GetAsync<T>(this IReadOnlyRemoteDataset<T> dataset, string id, CancellationToken cancellationToken = default) where T : notnull
        => dataset.GetAsync(id, new RemoteOperationOptions(), cancellationToken);

    /// <summary>
    /// Removes an existing entity from the remote dataset.
    /// </summary>
    /// <param name="dataset">The dataset to use for the operation.</param>
    /// <param name="entity">The entity to remove from the remote dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns when the operation is complete.</returns>
    /// <exception cref="ArgumentException">Thrown if the globally unique ID provided is not valid.</exception>
    /// <exception cref="EntityNotFoundException">Thrown if the entity does not exist on the server.</exception>
    /// <exception cref="ConflictException{T}">Thrown if there is a version mismatch on the server.</exception>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    public static Task RemoveAsync<T>(this IRemoteDataset<T> dataset, T entity, CancellationToken cancellationToken = default) where T : notnull
    {
        _ = Ensure.That(entity, nameof(entity)).IsNotNull();
        string id = EntityTypeCache.GetEntityId(entity) ?? throw new ArgumentNullException(nameof(entity));
        _ = Ensure.That(id, $"{nameof(entity)}.Id").IsEntityId();
        string? version = EntityTypeCache.GetEntityVersion(entity);
        return dataset.RemoveAsync(id, new RemoteOperationOptions() { RequiredVersion = version }, cancellationToken);
    }

    /// <summary>
    /// Replaces an existing entity in the remote dataset.
    /// </summary>
    /// <param name="dataset">The dataset to use for the operation.</param>
    /// <param name="entity">The entity to replace in the remote dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the replaced data when complete.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity provided is not valid.</exception>
    /// <exception cref="ConflictException{T}">Thrown if there is a version mismatch on the server.</exception>
    /// <exception cref="EntityNotFoundException">Thrown if the entity does not exist on the server.</exception>
    /// <exception cref="DatasyncException">Thrown if the response from the server does not indicate success.</exception>
    public static Task<T> ReplaceAsync<T>(this IRemoteDataset<T> dataset, T entity, CancellationToken cancellationToken = default) where T : notnull
    {
        _ = Ensure.That(entity, nameof(entity)).IsNotNull();
        string? version = EntityTypeCache.GetEntityVersion(entity);
        return dataset.ReplaceAsync(entity, new RemoteOperationOptions() { RequiredVersion = version }, cancellationToken);
    }
}
