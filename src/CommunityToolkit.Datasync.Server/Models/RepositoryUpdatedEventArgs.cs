// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// The event arguments used when a repository is updated.
/// </summary>
public readonly struct RepositoryUpdatedEventArgs(TableOperation operation, string entityName, object entity)
{
    /// <summary>
    /// The operation that was performed on the entity.
    /// </summary>
    public TableOperation Operation { get; } = operation;

    /// <summary>
    /// The name of the entity.
    /// </summary>
    public string EntityName { get; } = entityName;

    /// <summary>
    /// The updated entity value.  If the entity was deleted, the original entity value is returned.
    /// </summary>
    public object Entity { get; } = entity;

    /// <summary>
    /// The time the repository event was raised.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}
