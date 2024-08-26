// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;

namespace TodoApp.WinUI3.Database;

/// <summary>
/// An abstract class for working with offline entities.
/// </summary>
public abstract class OfflineClientEntity
{
    [Key]
    public string Id { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string Version { get; set; }
    public bool Deleted { get; set; }
}
