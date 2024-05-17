// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The potential change types that can be recorded in the <see cref="OfflineQueueEntity"/>.
/// </summary>
public enum EntityChangeType
{
    /// <summary>
    /// The change type is unknown (and hence skipped - this is always an error).
    /// </summary>
    Unknown,

    /// <summary>
    /// The entity is being added.
    /// </summary>
    Add,

    /// <summary>
    /// The entity is to be deleted.
    /// </summary>
    Delete,

    /// <summary>
    /// The entity is being updated.
    /// </summary>
    Update
}

/// <summary>
/// When an entity is modified while offline, the changes are stored in the <see cref="OfflineDbContext"/>
/// in an operations queue.  This entity is used to record what changes are pending push to the server.
/// </summary>
[Table("__datasync_opsqueue")]
[Index(nameof(EntityName))]
public sealed class OfflineQueueEntity
{
    /// <summary>
    /// The globally unique ID for the change.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// The type of the change (Add,  Delete, or Update).
    /// </summary>
    public EntityChangeType ChangeType { get; set; } = EntityChangeType.Unknown;

    /// <summary>
    /// The name of the entity or table that is being modified.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity that is being modified.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The replacement version of the entity that is being modified.
    /// </summary>
    public string ReplacementJsonEntityData { get; set; } = string.Empty;

    /// <summary>
    /// The original version of the entity that is being modified.
    /// </summary>
    public string OriginalJsonEntityEntity { get; set; } = string.Empty;
}
