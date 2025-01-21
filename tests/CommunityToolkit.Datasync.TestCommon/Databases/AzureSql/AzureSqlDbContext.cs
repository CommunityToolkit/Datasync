// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class AzureSqlDbContext(DbContextOptions<AzureSqlDbContext> options) : BaseDbContext<AzureSqlDbContext, AzureSqlEntityMovie>(options)
{
    public static AzureSqlDbContext CreateContext(string connectionString, ITestOutputHelper output = null, bool clearEntities = true)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        DbContextOptionsBuilder<AzureSqlDbContext> optionsBuilder = new DbContextOptionsBuilder<AzureSqlDbContext>()
            .UseSqlServer(connectionString)
            .EnableLogging(output);
        AzureSqlDbContext context = new(optionsBuilder.Options);

        context.InitializeDatabase(clearEntities);
        context.PopulateDatabase();
        return context;
    }

    internal void InitializeDatabase(bool clearEntities)
    {
        const string datasyncTrigger = @"
            CREATE OR ALTER TRIGGER [dbo].[{0}_datasync] ON [dbo].[{0}] AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE
                    [dbo].[{0}]
                SET
                    [UpdatedAt] = GETUTCDATE()
                WHERE
                    [Id] IN (SELECT [Id] FROM INSERTED);
            END
        ";

        Database.EnsureCreated();
        ExecuteRawSqlOnEachEntity(datasyncTrigger);

        if (clearEntities)
        {
            ExecuteRawSqlOnEachEntity("DELETE FROM [dbo].[{0}]");
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateOnly>().HaveConversion<DateOnlyConverter>().HaveColumnType("date");
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AzureSqlEntityMovie>().ToTable(tb => tb.HasTrigger("datasync_trigger"));
        base.OnModelCreating(modelBuilder);
    }

    internal class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
    {
        public DateOnlyConverter() : base(d => d.ToDateTime(TimeOnly.MinValue), d => DateOnly.FromDateTime(d))
        {
        }
    }
}
