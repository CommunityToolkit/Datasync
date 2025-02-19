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
        Options = new TableControllerOptions { EnableSoftDelete = true };
    }
}