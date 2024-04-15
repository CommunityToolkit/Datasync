// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Server.Private;

/// <summary>
/// An internal version of the repository pattern that throws an exception if it is used.  This is used as
/// a placeholder for a repository that is not set.
/// </summary>
/// <typeparam name="TEntity">The type of entity used in the repository.</typeparam>
[ExcludeFromCodeCoverage]
internal class Repository<TEntity> : IRepository<TEntity> where TEntity : ITableData
{
    /// <inheritdoc />
    public ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("The repository must be set within the table controller.");
    }

    /// <inheritdoc />
    public ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("The repository must be set within the table controller.");
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("The repository must be set within the table controller.");
    }

    /// <inheritdoc />
    public ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("The repository must be set within the table controller.");
    }

    /// <inheritdoc />
    public ValueTask ReplaceAsync(TEntity entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("The repository must be set within the table controller.");
    }
}
