// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Serialization;

/// <summary>
/// A representation of just the metadata for an entity.
/// </summary>
internal class EntityMetadata
{
    /// <summary>
    /// The globally unique ID of the entity.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The date/time stamp for the last updated state.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// The version of the entity.
    /// </summary>
    public string? Version { get; set; }
}
