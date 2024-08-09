// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
public record DatasyncOperation
{
    /// <summary>
    /// A unique ID for the operation.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The kind of operation that this entity represents.
    /// </summary>
    public required OperationKind Kind { get; set; }

    /// <summary>
    /// The current state of the operation.
    /// </summary>
    public required OperationState State { get; set; }

    /// <summary>
    /// The fully qualified name of the entity type.
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// The globally unique ID of the entity.
    /// </summary>
    public required string ItemId { get; set; }

    /// <summary>
    /// The JSON-encoded representation of the Item.
    /// </summary>
    public required string Item { get; set; }

    /// <summary>
    /// The sequence number for the operation.  This is incremented for each
    /// new operation to a different entity.
    /// </summary>
    public required int Sequence { get; set; }

    /// <summary>
    /// The version number for the operation.  This is incremented as multiple
    /// changes to the same entity are performed in between pushes.
    /// </summary>
    public required int Version { get; set; }
}
