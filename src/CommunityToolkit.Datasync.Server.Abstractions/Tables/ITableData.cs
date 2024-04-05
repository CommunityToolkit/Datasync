// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// Defines the metadata that must be present on an entity to be used
/// for data synchronization.
/// </summary>
/// <remarks>
/// Each entity supported by the Datasync Toolkit must implement the
/// proper metadata for data synchronization to work.
/// </remarks>
public interface ITableData : IEquatable<ITableData>
{
    /// <summary>
    /// A globally unique identifier for the entity.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// If <c>true</c>, the entity is deleted.
    /// </summary>
    /// <remarks>
    /// This entity supports soft-delete (which is required to inform other
    /// clients that the entity has been deleted and should be removed from
    /// the offline store).
    /// </remarks>
    bool Deleted { get; set; }

    /// <summary>
    /// The date/time of the last update to the entity.
    /// </summary>
    /// <remarks>
    /// This is used to support incremental synchronization so that bandwidth
    /// can be conserved.  This field must support millisecond resolution.
    /// </remarks>
    DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// The concurrency token for the entity.
    /// </summary>
    /// <remarks>
    /// This is an opaque value that is used to detect conflicts during updates.
    /// </remarks>
    byte[] Version { get; set; }
}
