// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace TodoApp.WinUI3.Database;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
        _ = Database.EnsureCreated();
    }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder = builder.UseSqlite("Data Source=:memory:");
        base.OnConfiguring(builder);
    }
}
