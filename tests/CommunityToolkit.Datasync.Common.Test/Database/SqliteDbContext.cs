// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;
using CommunityToolkit.Datasync.Server;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Common.Test.Database;

[ExcludeFromCodeCoverage]
public class SqliteDbContext(DbContextOptions<SqliteDbContext> options) : BaseDbContext<SqliteDbContext, SqliteEntityMovie>(options)
{
    public static SqliteDbContext CreateContext(ITestOutputHelper output = null)
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        DbContextOptionsBuilder<SqliteDbContext> optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .EnableLogging(output);
        SqliteDbContext context = new(optionsBuilder.Options) { Connection = connection };

        context.InitializeDatabase();
        return context;
    }

    public SqliteConnection Connection { get; set; }

    public void InitializeDatabase()
    {
        Database.EnsureCreated();
        ExecuteRawSqlOnEachEntity(@"DELETE FROM ""{0}""");
        PopulateDatabase();
    }

    #region SaveChanges
    public override int SaveChanges()
    {
        UpdateTrackedEntities();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTrackedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTrackedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateTrackedEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    #endregion

    internal void UpdateTrackedEntities()
    {
        ChangeTracker.DetectChanges();
        foreach (EntityEntry change in ChangeTracker.Entries().Where(m => m.State is EntityState.Added or EntityState.Modified))
        {
            if (change.Entity is ITableData movie)
            {
                movie.UpdatedAt = DateTimeOffset.UtcNow;
                movie.Version = Guid.NewGuid().ToByteArray();
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SqliteEntityMovie>()
            .Property(m => m.UpdatedAt).HasConversion(new SqliteDateTimeOffsetConverter());
        base.OnModelCreating(modelBuilder);
    }

    internal class SqliteDateTimeOffsetConverter : ValueConverter<DateTimeOffset, long>
    {
        public SqliteDateTimeOffsetConverter() : base(v => v.ToUnixTimeMilliseconds(), v => DateTimeOffset.FromUnixTimeMilliseconds(v))
        {
        }
    }
}
