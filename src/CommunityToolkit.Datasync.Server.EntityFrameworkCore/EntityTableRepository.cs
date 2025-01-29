// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore;

/// <summary>
/// An implementation of the <see cref="IRepository{TEntity}"/> interface that
/// stores data via an Entity Framework Core <see cref="DbContext"/>.
/// </summary>
/// <typeparam name="TEntity">The type of entity to store in the database.</typeparam>
public class EntityTableRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntityTableData
{
    /// <summary>
    /// The <see cref="DbContext"/> used for saving changes to the entity set.
    /// </summary>
    protected DbContext Context { get; }

    /// <summary>
    /// The entity set that is used for managing the entities.
    /// </summary>
    protected DbSet<TEntity> DataSet { get; }

    /// <summary>
    /// If <c>true</c>, then <c>UpdatedAt</c> is updated by the repository.
    /// </summary>
    private readonly bool shouldUpdateUpdatedAt;

    /// <summary>
    /// If <c>true</c>, then <c>Version</c> is updated by the repository.
    /// </summary>
    private readonly bool shouldUpdateVersion;

    /// <summary>
    /// Creates a new instance of the <see cref="EntityTableRepository{TEntity}"/> class, using the provided
    /// <see cref="DbContext"/> to store the entities."
    /// </summary>
    /// <param name="context">The database context representing the backend store.</param>
    /// <exception cref="ArgumentException">Thrown if the <typeparamref name="TEntity"/> is not registered in the <paramref name="context"/>.</exception>
    public EntityTableRepository(DbContext context)
    {
        Context = context;
        try
        {
            DataSet = context.Set<TEntity>();
            // This throws an InvalidOperationException if the set is not declared in the context.
            _ = DataSet.Local;
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException($"Unregistered entity type {typeof(TEntity).Name}", nameof(context));
        }

        this.shouldUpdateUpdatedAt = Attribute.IsDefined(typeof(TEntity).GetProperty(nameof(ITableData.UpdatedAt))!, typeof(UpdatedByRepositoryAttribute));
        this.shouldUpdateVersion = Attribute.IsDefined(typeof(TEntity).GetProperty(nameof(ITableData.Version))!, typeof(UpdatedByRepositoryAttribute));
    }

    /// <summary>
    /// The mechanism by which an Id is generated when one is not provided.
    /// </summary>
    public Func<TEntity, string> IdGenerator { get; set; } = _ => Guid.NewGuid().ToString("N");

    /// <summary>
    /// The mechanism by which a new version byte array is generated.
    /// </summary>
    public Func<byte[]> VersionGenerator { get; set; } = () => Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Retrieves an untracked version of an entity from the database.
    /// </summary>
    /// <param name="id">The ID of the entity to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns an untracked version of the entity when complete.</returns>
    protected Task<TEntity> GetEntityAsync(string id, CancellationToken cancellationToken = default)
        => DataSet.AsNoTracking().SingleAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Updates the managed properties for this entity if required.
    /// </summary>
    /// <param name="entity">The entity to be updated.</param>
    internal void UpdateManagedProperties(TEntity entity)
    {
        if (this.shouldUpdateUpdatedAt)
        {
            entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if (this.shouldUpdateVersion)
        {
            entity.Version = VersionGenerator.Invoke();
        }
    }

    /// <summary>
    /// Runs the inner part of an operation on the database, catching all the normal exceptions and reformatting them
    /// as appropriate.
    /// </summary>
    /// <param name="id">The ID of the entity being operated on.</param>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    /// <exception cref="HttpException">Thrown if a concurrency exception occurs.</exception>
    /// <exception cref="RepositoryException">Thrown if an error in the backend occurs.</exception>
    internal async Task WrapExceptionAsync(string id, Func<Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await action.Invoke().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new HttpException((int)HttpStatusCode.Conflict, ex.Message, ex) { Payload = await GetEntityAsync(id, cancellationToken).ConfigureAwait(false) };
        }
        catch (DbUpdateException ex)
        {
            throw new RepositoryException(ex.Message, ex);
        }
    }

    #region IRepository<TEntity> implementation
    /// <inheritdoc />
    public virtual ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(DataSet.AsNoTracking());

    /// <inheritdoc />
    [SuppressMessage("Performance", "CA1827:Do not use Count() or LongCount() when Any() can be used", Justification = "Not all EF providers support Any()")]
    public virtual async ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = IdGenerator.Invoke(entity);
        }

        await WrapExceptionAsync(entity.Id, async () =>
        {
            // We do not use Any() here because it is not supported by all providers (e.g. Cosmos)
            if (DataSet.Count(x => x.Id == entity.Id) > 0)
            {
                throw new HttpException((int)HttpStatusCode.Conflict) { Payload = await GetEntityAsync(entity.Id, cancellationToken).ConfigureAwait(false) };
            }

            UpdateManagedProperties(entity);
            _ = DataSet.Add(entity);
            _ = await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is required");
        }

        await WrapExceptionAsync(id, async () =>
        {
            TEntity storedEntity = await DataSet.FindAsync([id], cancellationToken).ConfigureAwait(false)
                ?? throw new HttpException((int)HttpStatusCode.NotFound);

            if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
            {
                throw new HttpException((int)HttpStatusCode.PreconditionFailed) { Payload = await GetEntityAsync(id, cancellationToken).ConfigureAwait(false) };
            }

            _ = DataSet.Remove(storedEntity);
            _ = await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is required");
        }

        TEntity entity = await DataSet.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false)
            ?? throw new HttpException((int)HttpStatusCode.NotFound);

        return entity;
    }

    /// <inheritdoc />
    public virtual async ValueTask ReplaceAsync(TEntity entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is required");
        }

        await WrapExceptionAsync(entity.Id, async () =>
        {
            TEntity storedEntity = await DataSet.FindAsync([entity.Id], cancellationToken).ConfigureAwait(false)
                ?? throw new HttpException((int)HttpStatusCode.NotFound);

            if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
            {
                throw new HttpException((int)HttpStatusCode.PreconditionFailed) { Payload = await GetEntityAsync(entity.Id, cancellationToken).ConfigureAwait(false) };
            }

            UpdateManagedProperties(entity);
            Context.Entry(storedEntity).CurrentValues.SetValues(entity);
            _ = await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
    {
        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async ValueTask<IList<TEntity>> ToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
    {
        return await query.ToListAsync(cancellationToken);
    }
    #endregion
}
