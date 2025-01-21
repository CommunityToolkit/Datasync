// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace Sample.Datasync.Server.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public async Task InitializeDatabaseAsync() 
    {
        _ = await Database.EnsureCreatedAsync();

        const string datasyncTrigger = @"
            CREATE OR ALTER TRIGGER [dbo].[{0}_datasync] ON [dbo].[{0}] AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE
                    [dbo].[{0}]
                SET
                    [UpdatedAt] = SYSUTCDATETIME()
                WHERE
                    [Id] IN (SELECT [Id] FROM INSERTED);
            END
        "
        ;

        // Install the above trigger to set the UpdatedAt field automatically on insert or update.
        foreach (IEntityType table in Model.GetEntityTypes())
        {
            string sql = string.Format(datasyncTrigger, table.GetTableName());
            _ = await Database.ExecuteSqlRawAsync(sql);
        }
    }

    [SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Model builder ignores return value.")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tells EF Core that the TodoItem entity has a trigger.
        modelBuilder.Entity<TodoItem>()
            .ToTable(tb => tb.HasTrigger("TodoItem_datasync"));

        // Tells EF Core that the TodoList entity has a trigger.
        modelBuilder.Entity<TodoList>()
            .ToTable(tb => tb.HasTrigger("TodoList_datasync"));

        base.OnModelCreating(modelBuilder);
    }
}
