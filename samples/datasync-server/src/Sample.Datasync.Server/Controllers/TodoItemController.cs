// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Sample.Datasync.Server.Db;

namespace Sample.Datasync.Server.Controllers;

[Route("tables/[controller]")]
[ApiExplorerSettings(IgnoreApi = false)]
public class TodoItemController : TableController<TodoItem>
{
    public TodoItemController(AppDbContext context) 
        : base(new EntityTableRepository<TodoItem>(context))
    {
        // UnsafeEntityLogging is intentionally left at its secure default (false).
        // This sample stores user-supplied TodoItem content (Title), so only the
        // entity ID is logged; the full serialized entity is never written to the logs.
        Options = new TableControllerOptions { UnsafeEntityLogging = false };
    }
}