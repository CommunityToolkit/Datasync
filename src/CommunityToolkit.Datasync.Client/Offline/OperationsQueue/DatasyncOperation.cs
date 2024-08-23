// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The kind of operation represented by a <see cref="DatasyncOperation"/> entity.
/// </summary>
public enum OperationKind
{
    /// <summary>The operation represents an addition to the entity set.</summary>
    Add,
    /// <summary>The operation represents a deletion from the entity set.</summary>
    Delete,
    /// <summary>The operation represents a replacement of the entity within the entity set.</summary>
    Replace
}

/// <summary>
/// The current state of the pending operation.
/// </summary>
public enum OperationState
{
    /// <summary>The operation has not been completed yet.</summary>
    Pending,
    /// <summary>The operation has been completed successfully.</summary>
    Completed,
    /// <summary>The operation failed.</summary>
    Failed
}

/// <summary>
/// An entity representing a pending operation against an entity set.
/// </summary>
[Index(nameof(ItemId), nameof(EntityType))]
public class DatasyncOperation
{
    /// <summary>
    /// A unique ID for the operation.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// The kind of operation that this entity represents.
    /// </summary>
    [Required]
    public required OperationKind Kind { get; set; }

    /// <summary>
    /// The current state of the operation.
    /// </summary>
    [Required]
    public required OperationState State { get; set; }

    /// <summary>
    /// The date/time of the last attempt.
    /// </summary>
    public DateTimeOffset? LastAttempt { get; set; }

    /// <summary>
    /// The HTTP Status Code for the last attempt.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// The fully qualified name of the entity type.
    /// </summary>
    [Required, MaxLength(255)]
    public required string EntityType { get; set; }

    /// <summary>
    /// The globally unique ID of the entity.
    /// </summary>
    [Required, MaxLength(126)]
    public required string ItemId { get; set; }

    /// <summary>
    /// The version of the entity currently downloaded from the service.
    /// </summary>
    [Required, MaxLength(126)]
    public required string EntityVersion { get; set; }

    /// <summary>
    /// The JSON-encoded representation of the Item.
    /// </summary>
    [Required, DataType(DataType.Text)]
    public required string Item { get; set; }

    /// <summary>
    /// The sequence number for the operation.  This is incremented for each
    /// new operation to a different entity.
    /// </summary>
    [DefaultValue(0L)]
    public required long Sequence { get; set; }

    /// <summary>
    /// The version number for the operation.  This is incremented as multiple
    /// changes to the same entity are performed in between pushes.
    /// </summary>
    [DefaultValue(0)]
    public required int Version { get; set; }
}
