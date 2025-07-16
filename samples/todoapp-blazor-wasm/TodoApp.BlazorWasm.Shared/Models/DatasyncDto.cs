// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TodoApp.BlazorWasm.Shared.Models;

/// <summary>
/// Represents a data transfer object (DTO) for a datasync client item in the application.
/// This class is used for communication between the client and server, containing all necessary
/// properties for datasync operations with the CommunityToolkit.Datasync framework.
/// </summary>
/// <remarks>
/// This DTO includes standard datasync properties such as timestamps, version control, and
/// soft delete functionality to support offline synchronization scenarios.
/// </remarks>
public abstract class DatasyncDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the todo item.
    /// </summary>
    /// <value>
    /// A string representing the unique ID of the todo item. This is typically a GUID
    /// converted to string format for compatibility with datasync operations.
    /// </value>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the todo item was created.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the item was first created.
    /// This timestamp includes timezone information for accurate synchronization across different time zones.
    /// </value>
    /// <remarks>
    /// This property is automatically managed by the datasync framework and should not
    /// be manually modified in most scenarios.
    /// </remarks>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the todo item was last updated.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing the most recent modification time.
    /// This timestamp includes timezone information for accurate synchronization across different time zones.
    /// </value>
    /// <remarks>
    /// This property is automatically updated by the datasync framework whenever
    /// the entity is modified and should not be manually set in most scenarios.
    /// </remarks>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the version identifier used for optimistic concurrency control.
    /// </summary>
    /// <value>
    /// A string representing the current version of the entity, typically used as an ETag
    /// for conflict resolution during synchronization operations.
    /// </value>
    /// <remarks>
    /// This property is managed by the datasync framework to handle concurrent updates
    /// and ensure data consistency. It should not be manually modified.
    /// </remarks>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the todo item has been soft-deleted.
    /// </summary>
    /// <value>
    /// <c>true</c> if the item has been marked for deletion but not physically removed;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property supports soft delete functionality in the datasync framework,
    /// allowing items to be marked as deleted without physically removing them from storage.
    /// This enables proper synchronization of delete operations across clients.
    /// </remarks>
    public bool Deleted { get; set; }
}
