// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sample.Datasync.Server.Db;

public class TodoItem : EntityTableData
{
    [Required, MinLength(1)]
    public string Text { get; set; } = string.Empty;

    public bool IsComplete { get; set; }
}
