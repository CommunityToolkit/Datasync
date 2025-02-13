using Microsoft.EntityFrameworkCore;

namespace TodoApp.Service.Database;

public class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public async Task InitializeDatabaseAsync()
    {
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
    }
}
