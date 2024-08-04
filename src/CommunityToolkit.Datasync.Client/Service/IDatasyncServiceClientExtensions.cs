// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A set of extension methods for the <see cref="IDatasyncServiceClient{TEntity}"/> class.
/// </summary>
public static class IDatasyncServiceClientExtensions
{
    /// <summary>
    /// Asynchronously returns the number of entities that will be returned by the provided query.
    /// </summary>
    /// <param name="source">The source service client.</param>
    /// <param name="query">Tne query to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    public static ValueTask<ServiceResponse<int>> CountAsync<TEntity>(this IReadOnlyDatasyncServiceClient<TEntity> source, IDatasyncQueryable<TEntity> query, CancellationToken cancellationToken = default) where TEntity : class
        => source.CountAsync(query, new DatasyncServiceOptions(), cancellationToken);

    /// <summary>
    /// Asynchronously returns the number of entities that are visible in the remote service.
    /// </summary>
    /// <param name="source">The source service client.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    public static ValueTask<ServiceResponse<int>> CountAsync<TEntity>(this IReadOnlyDatasyncServiceClient<TEntity> source, CancellationToken cancellationToken = default) where TEntity : class
        => source.CountAsync(source.AsQueryable(), new DatasyncServiceOptions(), cancellationToken);

    /// <summary>
    /// Asynchronously returns the requested entity.
    /// </summary>
    /// <param name="source">The source service client.</param>
    /// <param name="id">Tne globally unique ID of the entity to be retrieved.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the requested entity.</returns>
    public static ValueTask<ServiceResponse<TEntity>> GetAsync<TEntity>(this IReadOnlyDatasyncServiceClient<TEntity> source, string id, CancellationToken cancellationToken = default) where TEntity : class
        => source.GetAsync(id, new DatasyncServiceOptions(), cancellationToken);

    /// <summary>
    /// Asynchronously returns the number of entities that will be returned by the provided query.
    /// </summary>
    /// <param name="source">The source service client.</param>
    /// <param name="query">Tne query to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    public static ValueTask<ServiceResponse<long>> LongCountAsync<TEntity>(this IReadOnlyDatasyncServiceClient<TEntity> source, IDatasyncQueryable<TEntity> query, CancellationToken cancellationToken = default) where TEntity : class
        => source.LongCountAsync(query, new DatasyncServiceOptions(), cancellationToken);

    /// <summary>
    /// Asynchronously returns the number of entities that are visible in the remote service.
    /// </summary>
    /// <param name="source">The source service client.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    public static ValueTask<ServiceResponse<long>> LongCountAsync<TEntity>(this IReadOnlyDatasyncServiceClient<TEntity> source, CancellationToken cancellationToken = default) where TEntity : class
        => source.LongCountAsync(source.AsQueryable(), new DatasyncServiceOptions(), cancellationToken);

    /// <summary>
    /// Creates an array from the result of a table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An array of the results.</returns>
    public static ValueTask<TSource[]> ToArrayAsync<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source, CancellationToken cancellationToken = default) where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncArrayAsync(cancellationToken);

    /// <summary>
    /// Executes a query on the remote service, returning the results as an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the result set.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <returns>The async-enumerable sequence whose elements are pulled from the result set.</returns>
    public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source) where TSource : class
        => source.Query(source.AsQueryable());

    /// <summary>
    /// Executes a query on the remote service, returning the results as an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the result set.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <returns>The async-enumerable sequence whose elements are pulled from the result set.</returns>
    public static IAsyncPageable<TSource> ToAsyncPageable<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source) where TSource : class
        => source.Query(source.AsQueryable());

    /// <summary>
    /// Creates a dictionary from the result of a table query according to a specified key selector function.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the dictionary key computed for each element in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A dictionary mapping unique key values onto the corresponding result's element.</returns>
    public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IReadOnlyDatasyncServiceClient<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default)
        where TKey : notnull
        where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncDictionaryAsync(keySelector, cancellationToken);

    /// <summary>
    /// Creates a dictionary from the result of a table query according to a specified key selector function.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the dictionary key computed for each element in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">An equality comparer to compare keys.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A dictionary mapping unique key values onto the corresponding result's element.</returns>
    public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IReadOnlyDatasyncServiceClient<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, CancellationToken cancellationToken = default)
        where TKey : notnull
        where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncDictionaryAsync(keySelector, comparer, cancellationToken);

    /// <summary>
    /// Creates a hash set from the results of a table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A hash set containing all the elements of the source sequence.</returns>
    public static ValueTask<HashSet<TSource>> ToHashSetAsync<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source, CancellationToken cancellationToken = default) where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncHashSetAsync(cancellationToken);

    /// <summary>
    /// Creates a hash set from the results of a table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="comparer">An equality comparer to compare elements of the sequence.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A hash set containing all the elements of the source sequence.</returns>
    public static ValueTask<HashSet<TSource>> ToHashSetAsync<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source, IEqualityComparer<TSource>? comparer, CancellationToken cancellationToken = default) where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncHashSetAsync(comparer, cancellationToken);

    /// <summary>
    /// Creates a list from the result set of the table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A list containing all the elements of the source sequence.</returns>
    public static ValueTask<List<TSource>> ToListAsync<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source, CancellationToken cancellationToken = default) where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncListAsync(cancellationToken);

    /// <summary>
    /// Creates a <see cref="ConcurrentObservableCollection{T}"/> from the result set of the table query
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>A <see cref="ConcurrentObservableCollection{T}"/> containing all the elements of the source sequence.</returns>
    public static ValueTask<ConcurrentObservableCollection<TSource>> ToObservableCollection<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source, CancellationToken cancellationToken = default) where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncObservableCollectionAsync(cancellationToken);

    /// <summary>
    /// Updates a <see cref="ConcurrentObservableCollection{T}"/> from the result set of the table query.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The source service client.</param>
    /// <param name="collection">The <see cref="ConcurrentObservableCollection{T}"/> to update.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>The <see cref="ConcurrentObservableCollection{T}"/> passed in containing all the elements of the source sequence (replacing the old content).</returns>
    public static ValueTask<ConcurrentObservableCollection<TSource>> ToObservableCollection<TSource>(this IReadOnlyDatasyncServiceClient<TSource> source, ConcurrentObservableCollection<TSource> collection, CancellationToken cancellationToken = default) where TSource : class
        => source.ToAsyncEnumerable().ToDatasyncObservableCollectionAsync(collection, cancellationToken);

    /// <summary>
    /// Adds an entity to the remote service dataset.
    /// </summary>
    /// <param name="source"> The source service client.</param>
    /// <param name="entity">The entity to be added to the remote service dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response containing the added entity.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if the entity already exists in the remote service dataset.</exception>
    public static ValueTask<ServiceResponse<TEntity>> AddAsync<TEntity>(this IDatasyncServiceClient<TEntity> source, TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        => source.AddAsync(entity, new DatasyncServiceOptions(), cancellationToken);

    /// <summary>
    /// Removes an entity from the remote service dataset.
    /// </summary>
    /// <param name="source"> The source service client.</param>
    /// <param name="entity">The entity to be added to the remote service dataset.</param>
    /// <param name="force">If <c>true</c>, then the removal is forced and no exception is thrown for a missing entity.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    public static ValueTask<ServiceResponse> RemoveAsync<TEntity>(this IDatasyncServiceClient<TEntity> source, TEntity entity, bool force, CancellationToken cancellationToken = default) where TEntity : class
    {
        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity);
        ThrowIf.EntityIdIsInvalid(metadata.Id, nameof(metadata), because: "The value of the 'Id' property must be valid.");
        DatasyncServiceOptions options = new()
        {
            Version = force ? null : metadata.Version,
            ThrowIfMissing = !force
        };

        return source.RemoveAsync(metadata.Id!, options, cancellationToken);
    }

    /// <summary>
    /// Removes an entity from the remote service dataset.
    /// </summary>
    /// <param name="source"> The source service client.</param>
    /// <param name="entity">The entity to be added to the remote service dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    public static ValueTask<ServiceResponse> RemoveAsync<TEntity>(this IDatasyncServiceClient<TEntity> source, TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        => source.RemoveAsync(entity, false, cancellationToken);

    /// <summary>
    /// Replaced an entity in the remote service dataset.
    /// </summary>
    /// <param name="source"> The source service client.</param>
    /// <param name="entity">The entity to be replaced in the remote service dataset.</param>
    /// <param name="force">If <c>true</c>, then the replacement is forced.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response containing the replaced entity.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    public static ValueTask<ServiceResponse<TEntity>> ReplaceAsync<TEntity>(this IDatasyncServiceClient<TEntity> source, TEntity entity, bool force, CancellationToken cancellationToken = default) where TEntity : class
    {
        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity);
        ThrowIf.EntityIdIsInvalid(metadata.Id, nameof(metadata), because: "The value of the 'Id' property must be valid.");
        DatasyncServiceOptions options = new()
        {
            Version = force ? null : metadata.Version
        };

        return source.ReplaceAsync(entity, options, cancellationToken);
    }

    /// <summary>
    /// Replaced an entity in the remote service dataset.
    /// </summary>
    /// <param name="source"> The source service client.</param>
    /// <param name="entity">The entity to be replaced in the remote service dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response containing the replaced entity.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    public static ValueTask<ServiceResponse<TEntity>> ReplaceAsync<TEntity>(this IDatasyncServiceClient<TEntity> source, TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        => source.ReplaceAsync(entity, false, cancellationToken);
}
