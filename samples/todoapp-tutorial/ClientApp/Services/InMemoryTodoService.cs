using ClientApp.Interfaces;
using ClientApp.Models;
using System.Collections.Concurrent;

namespace ClientApp.Services;

public class InMemoryTodoService : ITodoService
{
    private readonly ConcurrentDictionary<string, TodoItem> items = [];

    public Task<TodoItem> AddTodoItemAsync(string title, CancellationToken cancellationToken = default)
    {
        TodoItem item = new() { Title = title };
        UpdateMetadata(item);
        items.TryAdd(item.Id, item);
        return Task.FromResult(item);
    }

    public Task<TodoItem?> GetTodoItemAsync(string id, CancellationToken cancellationToken = default)
    {
        items.TryGetValue(id, out TodoItem? item);
        return Task.FromResult(item);
    }

    public Task<List<TodoItem>> GetAllTodoItemsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(items.Values.ToList());
    }

    public Task<TodoItem> ReplaceTodoItemAsync(TodoItem updatedItem, CancellationToken cancellationToken = default)
    {
        UpdateMetadata(updatedItem);
        items.TryUpdate(updatedItem.Id, updatedItem, items[updatedItem.Id]);
        items[updatedItem.Id] = updatedItem;
        return Task.FromResult(updatedItem);
    }

    private static void UpdateMetadata(TodoItem item)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.Version = Guid.NewGuid().ToString("N");
    }
}