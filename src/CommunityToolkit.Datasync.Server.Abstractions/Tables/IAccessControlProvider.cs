// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// Defines the access permissions for a client to access the entities
/// within a repository.
/// </summary>
/// <remarks>
/// Do not implement this interface directly.  Instead, inherit from the <see cref="AccessControlProvider{TEntity}"/> class.
/// </remarks>
/// <typeparam name="TEntity">The type of entity stored within the table.</typeparam>
public interface IAccessControlProvider<TEntity> where TEntity : ITableData
{
    /// <summary>
    /// Returns a LINQ <see cref="Expression{TDelegate}"/> predicate to limit the data that the
    /// client can retrieve using a query operation.  Return null if you wish the client to see
    /// all data.
    /// </summary>
    /// <returns>An (optional) expression predicate to limit the data that the client can retrieve using a query operation.</returns>
    Expression<Func<TEntity, bool>>? GetDataView();

    /// <summary>
    /// Determines if the client is allowed to perform the <see cref="TableOperation"/> on the provided entity.
    /// </summary>
    /// <remarks>
    /// If the operation is a write (create, delete, replace), then the UnauthorizedStatusCode will be sent back to the client.  If the
    /// operation is a read, then NotFound will be sent back to the client.  If the TableOperation is Query, then the entity will be the
    /// default value and should be ignored.
    /// </remarks>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="entity">The entity that is being requested (null for queries).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns <c>true</c> if the operation is allowed, and <c>false</c> otherwise.</returns>
    ValueTask<bool> IsAuthorizedAsync(TableOperation operation, TEntity? entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// A hook to allow the developer to modify the entity prior to storage.
    /// </summary>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="entity">The entity that is being stored.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    ValueTask PreCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// A hook that is called after the entity is updated within the data store.
    /// </summary>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="entity">The entity that was stored.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    ValueTask PostCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken = default);
}
