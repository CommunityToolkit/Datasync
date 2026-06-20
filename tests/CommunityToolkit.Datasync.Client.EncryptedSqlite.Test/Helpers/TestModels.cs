// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite.Test.Helpers;

/// <summary>
/// A simple synchronizable entity used by the encrypted store tests.
/// </summary>
[ExcludeFromCodeCoverage]
public class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Version { get; set; }
}

/// <summary>
/// A plain (non-offline) EF Core context used to verify raw encryption behaviour.
/// </summary>
[ExcludeFromCodeCoverage]
public class PlainTodoContext(DbContextOptions<PlainTodoContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}

/// <summary>
/// An <see cref="OfflineDbContext"/> subclass used to verify that the encrypted store works with the offline client.
/// </summary>
[ExcludeFromCodeCoverage]
public class OfflineTodoContext(DbContextOptions<OfflineTodoContext> options) : OfflineDbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder.UseEndpoint(new Uri("https://localhost/tables/todoitem"));
    }
}
