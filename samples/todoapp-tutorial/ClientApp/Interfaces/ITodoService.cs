using ClientApp.Models;

namespace ClientApp.Interfaces;

/// <summary>
/// Service definition for the TodoItem service.
/// </summary>
public interface ITodoService
{
    Task<TodoItem?> GetTodoItemAsync(string id, CancellationToken cancellationToken = default);
    Task<List<TodoItem>> GetAllTodoItemsAsync(CancellationToken cancellationToken = default);
    Task<TodoItem> AddTodoItemAsync(string title, CancellationToken cancellationToken = default);
    Task<TodoItem> ReplaceTodoItemAsync(TodoItem updatedItem, CancellationToken cancellationToken = default);
}