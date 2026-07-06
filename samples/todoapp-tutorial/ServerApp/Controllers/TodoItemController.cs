using CommunityToolkit.Datasync.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServerApp.Models;

namespace ServerApp.Controllers;

[Route("tables/todoitem")]
public class TodoItemController : TableController<TodoItem>
{
    public TodoItemController(IRepository<TodoItem> repository, ILogger<TodoItemController> logger)
        : base(repository)
    {
        Logger = logger;
        Options = new TableControllerOptions
        {
            // Demonstrates entity serialization in the logs (entity ID at Information, full entity at Debug).
            // Only enable when the additional diagnostic detail is required and the log sink is secured,
            // as entity contents may contain PII, secrets, or other sensitive business data.
            UnsafeEntityLogging = true
        };
    }
}
