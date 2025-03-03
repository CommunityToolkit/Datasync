using CommunityToolkit.Datasync.Server;
using Microsoft.AspNetCore.Mvc;
using ServerApp.Models;

namespace ServerApp.Controllers;

[Route("tables/todoitem")]
public class TodoItemController(IRepository<TodoItem> repository) : TableController<TodoItem>(repository)
{
}