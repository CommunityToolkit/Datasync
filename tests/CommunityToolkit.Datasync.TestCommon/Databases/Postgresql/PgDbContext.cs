// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class PgDbContext(DbContextOptions<PgDbContext> options) : BaseDbContext<PgDbContext, PgEntityMovie>(options)
{
    public static async Task<PgDbContext> CreateContextAsync(string connectionString, ITestOutputHelper output = null, bool clearEntities = true)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        DbContextOptionsBuilder<PgDbContext> optionsBuilder = new DbContextOptionsBuilder<PgDbContext>()
            .UseNpgsql(connectionString)
            .EnableLogging(output);
        PgDbContext context = new(optionsBuilder.Options);

        await context.InitializeDatabaseAsync(clearEntities);
        await context.PopulateDatabaseAsync();
        return context;
    }

    internal async Task InitializeDatabaseAsync(bool clearEntities)
    {
        const string datasyncTrigger = """

            CREATE OR REPLACE FUNCTION {0}_datasync() RETURNS trigger AS $$
            BEGIN
                NEW."UpdatedAt" = NOW() AT TIME ZONE 'UTC';
                NEW."Version" = convert_to(gen_random_uuid()::text, 'UTF8');
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            CREATE OR REPLACE TRIGGER
                {0}_datasync
            BEFORE INSERT OR UPDATE ON
                "{0}"
            FOR EACH ROW EXECUTE PROCEDURE
                {0}_datasync();
        """;

        await Database.EnsureCreatedAsync();
        await ExecuteRawSqlOnEachEntityAsync(datasyncTrigger);

        if (clearEntities)
        {
            await ExecuteRawSqlOnEachEntityAsync(@"DELETE FROM ""{0}""");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PgEntityMovie>().Property(m => m.UpdatedAt)
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        // Ensures stable ordering across all database types.
        modelBuilder.Entity<PgEntityMovie>().Property(m => m.Title)
            .UseCollation("POSIX");

        base.OnModelCreating(modelBuilder);
    }
}
