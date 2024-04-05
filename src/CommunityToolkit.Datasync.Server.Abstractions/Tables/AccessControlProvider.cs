// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// The default implementation of the <see cref="IAccessControlProvider{TEntity}"/> interface that
/// allows the connecting client to do anything, but doesn't do anything extra.
/// </summary>
/// <typeparam name="TEntity">The type of entity stored within the table.</typeparam>
public class AccessControlProvider<TEntity> : IAccessControlProvider<TEntity> where TEntity : ITableData
{
    /// <inheritdoc />
    public virtual Expression<Func<TEntity, bool>>? GetDataView()
        => null;

    /// <inheritdoc />
    public virtual ValueTask<bool> IsAuthorizedAsync(TableOperation operation, TEntity? entity, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public virtual ValueTask PostCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public virtual ValueTask PreCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
