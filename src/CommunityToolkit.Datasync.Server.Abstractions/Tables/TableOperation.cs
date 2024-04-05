// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A list of valid operation types for a single operation on a table.
/// </summary>
/// <remarks>
/// This is used in an <see cref="IAccessControlProvides{TEntity}"/> to pass the
/// operation type being requested by the client.
/// </remarks>
public enum TableOperation
{
    /// <summary>
    /// An entity is being created.
    /// </summary>
    Create,

    /// <summary>
    /// An entity is being deleted.
    /// </summary>
    Delete,

    /// <summary>
    /// The data store is being queried.
    /// </summary>
    Query,

    /// <summary>
    /// An entity is being read.
    /// </summary>
    Read,

    /// <summary>
    /// An entity is being updated.
    /// </summary>
    Update
}
