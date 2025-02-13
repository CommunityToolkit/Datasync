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
    }
}
