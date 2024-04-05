// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Common.Test.Database;

[ExcludeFromCodeCoverage]
public class AzureSqlDbContext : BaseDbContext<AzureSqlDbContext, EntityMovie>
{
    public static AzureSqlDbContext CreateContext(string connectionString, ITestOutputHelper output = null)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        DbContextOptionsBuilder<AzureSqlDbContext> optionsBuilder = new DbContextOptionsBuilder<AzureSqlDbContext>()
            .UseSqlServer(connectionString)
            .EnableLogging(output);
        AzureSqlDbContext context = new(optionsBuilder.Options);

        context.InitializeDatabase();
        context.PopulateDatabase();
        return context;
    }

    public AzureSqlDbContext(DbContextOptions<AzureSqlDbContext> options) : base(options)
    {
    }

    internal void InitializeDatabase()
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
        ExecuteRawSqlOnEachEntity("DELETE FROM [dbo].[{0}]");
        ExecuteRawSqlOnEachEntity(datasyncTrigger);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateOnly>().HaveConversion<DateOnlyConverter>().HaveColumnType("date");
        base.ConfigureConventions(configurationBuilder);
    }

    internal class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
    {
        public DateOnlyConverter() : base(d => d.ToDateTime(TimeOnly.MinValue), d => DateOnly.FromDateTime(d))
        {
        }
    }
}
