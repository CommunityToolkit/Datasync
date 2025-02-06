// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.OpenApiService.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.OpenApiService.Models;

[ExcludeFromCodeCoverage]
public class ServiceDbContext(DbContextOptions<ServiceDbContext> options) : DbContext(options)
{
    public DbSet<KitchenSink> KitchenSinks => Set<KitchenSink>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public void InitializeDatabase()
    {
        bool created = Database.EnsureCreated();
        if (created && Database.IsSqlite())
        {
            this.EnableSqliteExtensions();
        }
    }
}