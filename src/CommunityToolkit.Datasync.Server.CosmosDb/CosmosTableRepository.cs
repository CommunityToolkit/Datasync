// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.CosmosDb.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net;
using System.Text;

namespace CommunityToolkit.Datasync.Server.CosmosDb;

/// <summary>
/// An implementation of the <see cref="IRepository{TEntity}"/> interface that
/// stores data via an Cosmos DB Container<see cref="Container"/>.
/// </summary>
/// <typeparam name="TEntity">The type of entity to store in the database.</typeparam>
public class CosmosTableRepository<TEntity> : IRepository<TEntity> where TEntity : CosmosTableData
{
    /// <summary>
    /// The <see cref="ICosmosTableOptions{TEntity}"/> for the entity set.
    /// </summary>
    protected ICosmosTableOptions<TEntity> Options { get; }
    /// <summary>
    /// The <see cref="Container"/> used for saving changes to the entity set.
    /// </summary>
    protected Container Container { get; }

    /// <inheritdoc />
    public async ValueTask<int> CountAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default)
        => await queryable.CountAsync(cancellationToken);
    /// <inheritdoc />
    public async ValueTask<IList<TEntity>> ToListAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default)
        => await queryable.ToListAsync(cancellationToken);

    /// <summary>
    /// Creates a new instance of the <see cref="CosmosTableRepository{TEntity}"/> class, using the provided
    /// <see cref="CosmosClient"/> to store the entities."
    /// </summary>
    /// <param name="client">The <see cref="CosmosClient" /> to access the container for this repository.</param>
    /// <param name="options">The <see cref="ICosmosTableOptions{TEntity}"/> for this repository.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <see cref="CosmosClient" /> or <see cref="ICosmosTableOptions{TEntity}"/> is null.</exception>
    public CosmosTableRepository(CosmosClient client, ICosmosTableOptions<TEntity> options)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        Options = options ?? throw new ArgumentNullException(nameof(options));

        Container = client.GetContainer(options.DatabaseId, options.ContainerId);

    }

    /// <summary>
    /// Retrieves an untracked version of an entity from the database.
    /// </summary>
    /// <param name="entityId">The ID of the entity to retrieve.</param>
    /// <param name="partitionKey">The partition key for the entity.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns an untracked version of the entity when complete.</returns>
    /// <exception cref="CosmosException">Thrown if an error in the backend occurs.</exception>
    protected async Task<TEntity?> GetEntityAsync(string entityId, PartitionKey partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Container.ReadItemAsync<TEntity>(entityId, partitionKey, cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Updates the managed properties for this entity if required.
    /// </summary>
    /// <param name="entity">The entity to be updated.</param>
    internal void UpdateManagedProperties(TEntity entity)
    {
        if (Options.ShouldUpdateTimestamp)
        {
            entity.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Runs the inner part of an operation on the database, catching all the normal exceptions and reformatting them
    /// as appropriate.
    /// </summary>
    /// <param name="id">The ID of the entity being operated on.</param>
    /// <param name="partitionKey">The partition key for the entity.</param>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    /// <exception cref="HttpException">Thrown if a concurrency exception occurs.</exception>
    /// <exception cref="CosmosException">Thrown if an error in the backend occurs.</exception>
    internal async Task WrapExceptionAsync(string id, PartitionKey partitionKey, Func<Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await action.Invoke().ConfigureAwait(false);
        }
        catch (CosmosException ex)
        {
            throw new HttpException((int)ex.StatusCode, ex.Message, ex) { Payload = await GetEntityAsync(id, partitionKey, cancellationToken).ConfigureAwait(false) };
        }
    }

    #region IRepository<TEntity> implementation
    /// <inheritdoc />
    public virtual ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Container.GetItemLinqQueryable<TEntity>().Where(Options.QueryablePredicate()).AsQueryable());

    /// <inheritdoc />
    public virtual async ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = Options.IdGenerator.Invoke(entity);
        }

        string id = Options.GetPartitionKey(entity, out PartitionKey partitionKey);

        await WrapExceptionAsync(id, partitionKey, async () =>
        {
            UpdateManagedProperties(entity);

            ItemResponse<TEntity> response = await Container.CreateItemAsync(entity, partitionKey, cancellationToken: cancellationToken);

            entity.ETag = response.Resource.ETag;
            entity.UpdatedAt = response.Resource.UpdatedAt;

        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is required");
        }

        if (Options.TryParsePartitionKey(id, out string entityId, out PartitionKey partitionKey) == false)
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is not in the correct format");
        }

        await WrapExceptionAsync(id, partitionKey, async () =>
        {
            ItemRequestOptions? requestOptions = null;

            if (version?.Length > 0)
            {
                requestOptions = new ItemRequestOptions()
                {
                    IfMatchEtag = Encoding.UTF8.GetString(version)
                };
            }

            _ = await Container.DeleteItemAsync<TEntity>(entityId, partitionKey, requestOptions, cancellationToken);

        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is required");
        }

        if(Options.TryParsePartitionKey(id, out string entityId, out PartitionKey partitionKey) == false)
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is not in the correct format");
        }

        return await GetEntityAsync(entityId, partitionKey, cancellationToken).ConfigureAwait(false) ??
            throw new HttpException((int)HttpStatusCode.NotFound, $"Entity with id {id} not found");
    }

    /// <inheritdoc />
    public virtual async ValueTask ReplaceAsync(TEntity entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is required");
        }

        string id = Options.GetPartitionKey(entity, out PartitionKey partitionKey);

        await WrapExceptionAsync(id, partitionKey, async () =>
        {
            UpdateManagedProperties(entity);

            ItemRequestOptions? requestOptions = null;

            if (version?.Length > 0)
            {
                requestOptions = new ItemRequestOptions()
                {
                    IfMatchEtag = Encoding.UTF8.GetString(version)
                };
            }

            _ = await Container.ReplaceItemAsync(entity, id, partitionKey, requestOptions, cancellationToken);

        }, cancellationToken).ConfigureAwait(false);
    }
    #endregion
}
