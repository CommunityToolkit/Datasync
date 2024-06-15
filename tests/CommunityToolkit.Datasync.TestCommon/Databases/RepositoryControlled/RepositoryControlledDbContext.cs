// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class RepositoryControlledDbContext(DbContextOptions<RepositoryControlledDbContext> options) 
    : BaseDbContext<RepositoryControlledDbContext, RepositoryControlledEntityMovie>(options)
{
    public static RepositoryControlledDbContext CreateContext(ITestOutputHelper output = null)
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        DbContextOptionsBuilder<RepositoryControlledDbContext> optionsBuilder = new DbContextOptionsBuilder<RepositoryControlledDbContext>()
            .UseSqlite(connection)
            .EnableLogging(output);
        RepositoryControlledDbContext context = new(optionsBuilder.Options) { Connection = connection };

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RepositoryControlledEntityMovie>()
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
