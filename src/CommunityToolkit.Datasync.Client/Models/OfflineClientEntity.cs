// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The base class for entities that are stored in an offline database.
/// </summary>
public abstract class OfflineClientEntity
{
    /// <summary>
    /// The globally unique ID for the entity.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The date/time that the entity was last updated on the server.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The version of the entity on the server.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// If <c>true</c>, the entity is deleted on the server.
    /// </summary>
    public bool Deleted { get; set; }
}
