// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using Microsoft.AspNetCore.Mvc;
using Sample.Datasync.Server.SingleContainer.Models;

namespace Sample.Datasync.Server.Controllers;

[Route("tables/[controller]")]
public class TodoListController : TableController<TodoList>
{
    public TodoListController(IRepository<TodoList> repository) : base(repository)
    {
        // UnsafeEntityLogging is intentionally left at its secure default (false).
        // This sample stores user-supplied TodoList content (Title), so only the
        // entity ID is logged; the full serialized entity is never written to the logs.
        Options = new TableControllerOptions { EnableSoftDelete = true, UnsafeEntityLogging = false };
    }
}