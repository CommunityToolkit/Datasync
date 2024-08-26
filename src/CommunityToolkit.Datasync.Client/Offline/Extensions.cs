// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// A set of extension methods for the <see cref="DbSet{TEntity}"/> objects.
/// </summary>
public static class Extensions
{
    /// <summary>
    ///     Pulls the changes from the remote service for the specified dataset
    /// </summary>
    /// <param name="dataset">The dataset to pull from the remote service.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    public static Task<PullResult> PullAsync<TEntity>(this DbSet<TEntity> dataset, CancellationToken cancellationToken = default) where TEntity : class
    => dataset.PullAsync(new PullOptions(), cancellationToken);

    /// <summary>
    ///     Pulls the changes from the remote service for the specified dataset
    /// </summary>
    /// <param name="dataset">The dataset to pull from the remote service.</param>
    /// <param name="options">The options to use on this pull request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    public static Task<PullResult> PullAsync<TEntity>(this DbSet<TEntity> dataset, PullOptions options, CancellationToken cancellationToken = default) where TEntity : class
    {
        DbContext context = dataset.GetService<ICurrentDbContext>().Context;
        if (context is OfflineDbContext offlineContext)
        {
            return offlineContext.PullAsync([typeof(TEntity)], options, cancellationToken);
        }
        else
        {
            throw new DatasyncException($"Provided dataset is not a part of an {nameof(OfflineDbContext)}");
        }
    }

    /// <summary>
    ///     Pulls the changes from the remote service for all synchronizable entities.
    /// </summary>
    /// <param name="context">The offline database context to use.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    [ExcludeFromCodeCoverage]
    public static Task<PullResult> PullAsync(this OfflineDbContext context, CancellationToken cancellationToken = default)
        => context.PullAsync(context.QueueManager.GetSynchronizableEntityTypes(), new PullOptions(), cancellationToken);

    /// <summary>
    ///     Pulls the changes from the remote service for all synchronizable entities.
    /// </summary>
    /// <param name="context">The offline database context to use.</param>
    /// <param name="options">The options to use on this pull request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    [ExcludeFromCodeCoverage]
    public static Task<PullResult> PullAsync(this OfflineDbContext context, PullOptions options, CancellationToken cancellationToken = default)
        => context.PullAsync(context.QueueManager.GetSynchronizableEntityTypes(), options, cancellationToken);

    /// <summary>
    ///     Pulls the changes from the remote service for the specified synchronizable entities.
    /// </summary>
    /// <param name="context">The offline database context to use.</param>
    /// <param name="entityTypes">The list of entity types to pull.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    [ExcludeFromCodeCoverage]
    public static Task<PullResult> PullAsync(this OfflineDbContext context, IEnumerable<Type> entityTypes, CancellationToken cancellationToken = default)
        => context.PullAsync(entityTypes, new PullOptions(), cancellationToken);

    /// <summary>
    ///     Pushes the pending operations against the remote service for the provided dataset
    /// </summary>
    /// <param name="dataset">The data set to use for scoping the push operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    public static Task<PushResult> PushAsync<TEntity>(this DbSet<TEntity> dataset, CancellationToken cancellationToken = default) where TEntity : class
        => dataset.PushAsync(new PushOptions(), cancellationToken);

    /// <summary>
    ///     Pushes the pending operations against the remote service for the provided dataset
    /// </summary>
    /// <param name="dataset">The data set to use for scoping the push operation.</param>
    /// <param name="options">The options to use for this push operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    public static Task<PushResult> PushAsync<TEntity>(this DbSet<TEntity> dataset, PushOptions options, CancellationToken cancellationToken = default) where TEntity : class
    {
        DbContext context = dataset.GetService<ICurrentDbContext>().Context;
        if (context is OfflineDbContext offlineContext)
        {
            return offlineContext.PushAsync([typeof(TEntity)], options, cancellationToken);
        }
        else
        {
            throw new DatasyncException($"Provided dataset is not a part of an {nameof(OfflineDbContext)}");
        }
    }

    /// <summary>
    ///     Pushes the pending operations against the remote service for the full set of synchronizable entity types.
    /// </summary>
    /// <param name="context">The source <see cref="OfflineDbContext"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    [ExcludeFromCodeCoverage]
    public static Task<PushResult> PushAsync(this OfflineDbContext context, CancellationToken cancellationToken = default)
        => context.PushAsync(context.QueueManager.GetSynchronizableEntityTypes(), new PushOptions(), cancellationToken);

    /// <summary>
    ///     Pushes the pending operations against the remote service for the full set of synchronizable entity types.
    /// </summary>
    /// <param name="context">The source <see cref="OfflineDbContext"/>.</param>
    /// <param name="pushOptions">The options for this push operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    [ExcludeFromCodeCoverage]
    public static Task<PushResult> PushAsync(this OfflineDbContext context, PushOptions pushOptions, CancellationToken cancellationToken = default)
        => context.PushAsync(context.QueueManager.GetSynchronizableEntityTypes(), pushOptions, cancellationToken);

    /// <summary>
    ///     Pushes the pending operations against the remote service for the provided set of entity types.
    /// </summary>
    /// <param name="context">The source <see cref="OfflineDbContext"/>.</param>
    /// <param name="entityTypes">The list of entity types in scope for this push operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    [ExcludeFromCodeCoverage]
    public static Task<PushResult> PushAsync(this OfflineDbContext context, IEnumerable<Type> entityTypes, CancellationToken cancellationToken = default)
        => context.PushAsync(entityTypes, new PushOptions(), cancellationToken);
}
