// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace TodoApp.BlazorWasm.Shared.Models;

/// <summary>
/// Represents a data transfer object (DTO) for a todo item in the Blazor WebAssembly application.
/// This class is used for communication between the client and server, containing all necessary
/// properties for datasync operations with the CommunityToolkit.Datasync framework.
/// </summary>
public class TodoItemDto : DatasyncDto
{
    /// <summary>
    /// Gets or sets the title or description of the todo item.
    /// </summary>
    /// <value>
    /// The title text of the todo item. Must be between 1 and 255 characters in length.
    /// </value>
    /// <remarks>
    /// This property is required and has validation attributes to ensure the title
    /// is not empty and does not exceed the maximum length limit.
    /// </remarks>
    [Required, StringLength(255, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether the todo item has been completed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the todo item is completed; otherwise, <c>false</c>.
    /// </value>
    public bool Completed { get; set; }
}
