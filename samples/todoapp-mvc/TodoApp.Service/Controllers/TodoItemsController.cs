using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Service.Database;

namespace TodoApp.Service.Controllers;

[Route("tables/todoitems")]
public class TodoItemsController : TableController<TodoItem>
{
    public TodoItemsController(TodoContext context) : base()
    {
        Repository = new EntityTableRepository<TodoItem>(context);

        // UnsafeEntityLogging is intentionally left at its secure default (false).
        // This sample stores user-supplied TodoItem content, so only the entity ID
        // is logged; the full serialized entity is never written to the logs.
        Options = new TableControllerOptions { UnsafeEntityLogging = false };
    }
}
