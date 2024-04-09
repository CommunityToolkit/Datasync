// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace CommunityToolkit.Datasync.Server.Automapper;

/// <summary>
/// An implementation of <see cref="IRepository{T}"/> that uses AutoMapper to map between the
/// entity and DTO types.
/// </summary>
/// <typeparam name="TEntity">The type used by the inner repository for storage to the database.</typeparam>
/// <typeparam name="Tdto">The tpe used by the table controller for communication with the client.</typeparam>
/// <remarks>
/// Creates a new <see cref="MappedTableRepository{TEntity, Tdto}"/> instance that uses the provided
/// <paramref name="mapper"/> to map entities from the inner repository to DTOs for the table controller.
/// </remarks>
/// <param name="mapper">The <see cref="IMapper"/> to use for converting between DTO and entity types.</param>
/// <param name="repository">The <see cref="IRepository{TEntity}"/> to use for storing the entities.</param>
public class MappedTableRepository<TEntity, Tdto>(IMapper mapper, IRepository<TEntity> repository) : IRepository<Tdto>
    where TEntity : ITableData
    where Tdto : ITableData
{
    #region IRepository<Tdto> implementation
    /// <inheritdoc />
    public async ValueTask<IQueryable<Tdto>> AsQueryableAsync(CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = await repository.AsQueryableAsync(cancellationToken).ConfigureAwait(false);
        return queryable.ProjectTo<Tdto>(mapper.ConfigurationProvider);
    }

    /// <inheritdoc />
    public async ValueTask CreateAsync(Tdto dto, CancellationToken cancellationToken = default)
    {
        TEntity entity = mapper.Map<Tdto, TEntity>(dto);
        await repository.CreateAsync(entity, cancellationToken).ConfigureAwait(false);
        _ = mapper.Map(entity, dto);
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        await repository.DeleteAsync(id, version, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Tdto> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        TEntity entity = await repository.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        return mapper.Map<TEntity, Tdto>(entity);
    }

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(Tdto dto, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        TEntity entity = mapper.Map<Tdto, TEntity>(dto);
        await repository.ReplaceAsync(entity, version, cancellationToken);
        _ = mapper.Map(entity, dto);
    }
    #endregion
}
