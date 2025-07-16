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
public class TodoListController : TableController<TodoList>
{
    public TodoListController(AppDbContext context) : base(new EntityTableRepository<TodoList>(context))
    {
        Options = new TableControllerOptions { EnableSoftDelete = true };
    }
}