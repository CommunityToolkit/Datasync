// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// A set of extension methods when using offline to allow you to synchronize
/// a <see cref="DbSet{TEntity}"/> with a remote service.
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    ///     Pushes entities from the requested entity type to the remote service.
    /// </summary>
    /// <param name="set">The <see cref="DbSet{TEntity}"/> to use for synchronization.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    public static Task<PushOperationResult> PushAsync<TEntity>(this DbSet<TEntity> set, CancellationToken cancellationToken = default) where TEntity : class
        => PushAsync(set, new PushOptions(), cancellationToken);

    /// <summary>
    ///     Pushes entities from the requested entity type to the remote service.
    /// </summary>
    /// <param name="set">The <see cref="DbSet{TEntity}"/> to use for synchronization.</param>
    /// <param name="pushOptions">The options to use for this push operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    public static Task<PushOperationResult> PushAsync<TEntity>(this DbSet<TEntity> set, PushOptions pushOptions, CancellationToken cancellationToken = default) where TEntity : class
    {
        ICurrentDbContext context = set.GetService<ICurrentDbContext>();
        if (context.Context is OfflineDbContext offlineContext)
        {
            Type[] entityTypes = [typeof(TEntity)];
            return offlineContext.PushAsync(entityTypes, pushOptions, cancellationToken);
        }
        else
        {
            throw new DatasyncException("DbContext for this dataset is not an OfflineDbContext.");
        }
    }
}
