// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline.Internal;

/// <summary>
/// A model class representing a database update operation.
/// </summary>
internal class DatabaseUpdateOperation(Type entityType, object entity)
{
    /// <summary>
    /// The type of the entity.
    /// </summary>
    public Type EntityType { get; } = entityType;

    /// <summary>
    /// The entity value.
    /// </summary>
    public object Entity { get; } = entity;
}
