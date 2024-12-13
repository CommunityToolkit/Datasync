// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;

namespace TodoApp.Avalonia.Database;

/// <summary>
/// An abstract class for working with offline entities.
/// </summary>
public abstract class OfflineClientEntity
{
    /// <summary>
    /// Gets or sets the ID of this item.
    /// </summary>
    /// <remarks>
    /// The default is <see cref="Guid.NewGuid"/>.
    /// </remarks>
   
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the version info.
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets whether the item was deleted.
    /// </summary>
    public bool Deleted { get; set; }
}