// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TodoApp.BlazorWasm.Server.Database;

/// <summary>
/// Represents a todo item entity for the server-side database in the Blazor WebAssembly application.
/// This class inherits from <see cref="EntityTableData"/> to provide datasync capabilities
/// with the CommunityToolkit.Datasync.Server framework.
/// </summary>
/// <remarks>
/// <para>
/// This entity class is used by Entity Framework Core for database operations and by the
/// datasync framework for synchronization with client applications. It includes automatic
/// ID generation and validation attributes to ensure data integrity.
/// </para>
/// <para>
/// The class is mapped to a database table and exposed through the TodoItemsController
/// for RESTful API operations and real-time synchronization.
/// </para>
/// </remarks>
/// <seealso cref="EntityTableData"/>
/// <seealso cref="TodoApp.BlazorWasm.Shared.Models.TodoItemDto"/>
public class TodoItem : EntityTableData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TodoItem"/> class.
    /// </summary>
    /// <remarks>
    /// The constructor automatically generates a unique identifier using a GUID
    /// formatted as a 32-character hexadecimal string without hyphens (format "N").
    /// This ensures each todo item has a unique ID when created.
    /// </remarks>
    public TodoItem() : base()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Gets or sets the title or description of the todo item.
    /// </summary>
    /// <value>
    /// The title text of the todo item. Must be between 1 and 255 characters in length.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property is required and cannot be null or empty. The validation attributes
    /// ensure that the title has a minimum length of 1 character and does not exceed
    /// 255 characters to maintain database compatibility and user experience.
    /// </para>
    /// <para>
    /// The title represents the main content or description of what needs to be accomplished
    /// in this todo item.
    /// </para>
    /// </remarks>
    /// <exception cref="ValidationException">
    /// Thrown during model validation if the title is null, empty, or exceeds the maximum length.
    /// </exception>
    [Required, StringLength(255, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether the todo item has been completed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the todo item is completed; otherwise, <c>false</c>.
    /// The default value is <c>false</c> for newly created items.
    /// </value>
    /// <remarks>
    /// This boolean flag tracks the completion status of the todo item.
    /// When set to <c>true</c>, it indicates that the task has been finished.
    /// This property is commonly used for filtering and displaying items in different states.
    /// </remarks>
    public bool Completed { get; set; }
}
