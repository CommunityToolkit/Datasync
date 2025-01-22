// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class MysqlDbContext(DbContextOptions<MysqlDbContext> options) : BaseDbContext<MysqlDbContext, MysqlEntityMovie>(options)
{
    public static MysqlDbContext CreateContext(string connectionString, ITestOutputHelper output = null, bool clearEntities = true)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        DbContextOptionsBuilder<MysqlDbContext> optionsBuilder = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseMySql(connectionString: connectionString, serverVersion: ServerVersion.AutoDetect(connectionString))
            .EnableLogging(output);
        MysqlDbContext context = new(optionsBuilder.Options);

        context.InitializeDatabase(clearEntities);
        context.PopulateDatabase();
        return context;
    }

    internal void InitializeDatabase(bool clearEntities)
    {
        Database.EnsureCreated();

        if (clearEntities)
        {
            ExecuteRawSqlOnEachEntity(@"DELETE FROM {0}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MysqlEntityMovie>().Property(m => m.UpdatedAt)
            .ValueGeneratedOnAddOrUpdate();

        modelBuilder.Entity<MysqlEntityMovie>().Property(m => m.Version)
            .IsRowVersion();

        base.OnModelCreating(modelBuilder);
    }
}
