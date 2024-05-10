// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace Sample.Datasync.Server.Db;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {

    }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public async Task InitializeDatabaseAsync() 
    {
        await Database.EnsureCreatedAsync();
    }
}
