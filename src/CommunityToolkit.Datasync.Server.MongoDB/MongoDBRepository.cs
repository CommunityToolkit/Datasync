// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CommunityToolkit.Datasync.Server.MongoDB;

/// <summary>
/// A repository implementation that stored data in a LiteDB database.
/// </summary>
/// <typeparam name="TEntity">The entity type to store in the database.</typeparam>
public class MongoDBRepository<TEntity> : IRepository<TEntity> where TEntity : MongoTableData
{
    /// <summary>
    /// Creates a new <see cref="MongoDBRepository{TEntity}"/> using the provided MongoDB database.
    /// </summary>
    /// <remarks>
    /// The collection name is based on the entity type.
    /// </remarks>
    /// <param name="database">The <see cref="IMongoDatabase"/> to use for storing entities.</param>
    public MongoDBRepository(IMongoDatabase database) : this(database.GetCollection<TEntity>(typeof(TEntity).Name.ToLowerInvariant() + "s"))
    {
    }

    /// <summary>
    /// Creates a new <see cref="MongoDBRepository{TEntity}"/> using the provided database connection
    /// and collection name.
    /// </summary>
    /// <param name="collection">The <see cref="IMongoCollection{TDocument}"/> to use for storing entities.</param>
    public MongoDBRepository(IMongoCollection<TEntity> collection)
    {
        Collection = collection;
        // TODO: Ensure that there is an index on the right properties.
    }

    /// <summary>
    /// The collection within the LiteDb database that stores the entities.
    /// </summary>
    public virtual IMongoCollection<TEntity> Collection { get; }

    /// <summary>
    /// The mechanism by which an Id is generated when one is not provided.
    /// </summary>
    public Func<TEntity, string> IdGenerator { get; set; } = _ => Guid.NewGuid().ToString("N");

    /// <summary>
    /// The mechanism by which a new version byte array is generated.
    /// </summary>
    public Func<byte[]> VersionGenerator { get; set; } = () => Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Updates the system properties for the provided entity on write.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    protected void UpdateEntity(TEntity entity)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.Version = VersionGenerator.Invoke();
    }

    /// <summary>
    /// Checks that the provided ID is valid.
    /// </summary>
    /// <param name="id">The ID of the entity to check.</param>
    /// <exception cref="HttpException">Thrown if the ID is not valid.</exception>
    protected static void CheckIdIsValid(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException(HttpStatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Returns a filter definition for finding a single document.
    /// </summary>
    /// <param name="id">The ID of the document to find.</param>
    /// <returns>The filter definition to find the document.</returns>
    protected FilterDefinition<TEntity> GetFilterById(string id)
        => Builders<TEntity>.Filter.Eq(x => x.Id, id);

    /// <summary>
    /// Returns the document with the provided ID, or null if it doesn't exist.
    /// </summary>
    /// <param name="id">The ID of the document to find.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The document, or null if not found.</returns>
    protected async ValueTask<TEntity?> FindDocumentByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(GetFilterById(id)).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Collection.AsQueryable());

    /// <inheritdoc/>
    public virtual async ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = IdGenerator.Invoke(entity);
        }

        TEntity? existingEntity = await FindDocumentByIdAsync(entity.Id, cancellationToken).ConfigureAwait(false);
        if (existingEntity is not null)
        {
            throw new HttpException(HttpStatusCodes.Status409Conflict) { Payload = existingEntity };
        }

        UpdateEntity(entity);
        await Collection.InsertOneAsync(entity, options: null, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        CheckIdIsValid(id);

        TEntity storedEntity = await FindDocumentByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new HttpException(HttpStatusCodes.Status404NotFound);
        if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
        {
            throw new HttpException(HttpStatusCodes.Status412PreconditionFailed) { Payload = storedEntity };
        }

        DeleteResult result = await Collection.DeleteOneAsync(GetFilterById(id), cancellationToken);
        if (result.DeletedCount == 0)
        {
            throw new HttpException(HttpStatusCodes.Status404NotFound);
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        CheckIdIsValid(id);

        return await FindDocumentByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new HttpException(HttpStatusCodes.Status404NotFound);
    }

    /// <inheritdoc/>
    public virtual async ValueTask ReplaceAsync(TEntity entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        CheckIdIsValid(entity.Id);

        TEntity storedEntity = await FindDocumentByIdAsync(entity.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new HttpException(HttpStatusCodes.Status404NotFound);
        if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
        {
            throw new HttpException(HttpStatusCodes.Status412PreconditionFailed) { Payload = storedEntity };
        }

        UpdateEntity(entity);
        ReplaceOptions<TEntity> options = new() { IsUpsert = false };
        ReplaceOneResult result = await Collection.ReplaceOneAsync(GetFilterById(entity.Id), entity, options, cancellationToken);
        if (result.IsModifiedCountAvailable && result.ModifiedCount == 0)
        {
            throw new HttpException(HttpStatusCodes.Status404NotFound);
        }
    }
}
