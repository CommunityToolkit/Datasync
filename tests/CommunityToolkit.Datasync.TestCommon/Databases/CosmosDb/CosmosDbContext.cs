// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class CosmosDbContext(DbContextOptions<CosmosDbContext> options) : BaseDbContext<CosmosDbContext, CosmosEntityMovie>(options)
{
    public static CosmosDbContext CreateContext(string connectionString, ITestOutputHelper output = null, bool clearEntities = true)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        DbContextOptionsBuilder<CosmosDbContext> optionsBuilder = new DbContextOptionsBuilder<CosmosDbContext>()
            .UseCosmos(connectionString, databaseName: "unittests")
            .EnableLogging(output);
        CosmosDbContext context = new(optionsBuilder.Options);

        context.InitializeDatabase(clearEntities);
        context.PopulateDatabase();
        return context;
    }

    internal void InitializeDatabase(bool clearEntities)
    {
        if (clearEntities)
        {
            // NOTE: sync-over-async is used here.  This is bad, but it is only used in the test suite.
            // The test suite is not performance sensitive, so this is acceptable.
            List<CosmosEntityMovie> movies = Movies.ToListAsync().Result;
            RemoveRange(movies);
            SaveChanges();
        }
    }

    // (Issue #199) - remove the sync over async capabilities introduced in .NET 9
    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    optionsBuilder.ConfigureWarnings(w => w.Ignore(CosmosEventId.SyncNotSupported));
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CosmosEntityMovie>(builder =>
        {
            builder.ToContainer("Movies");
            builder.HasNoDiscriminator();
            builder.HasPartitionKey(model => model.Id);
            builder.Property(model => model.EntityTag).IsETagConcurrency();

            // Note that the composite indices needed for Cosmos are defined in the bicep
            // See infra/modules/cosmos.bicep

        });
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateOnly>().HaveConversion<DateOnlyConverter>();
        base.ConfigureConventions(configurationBuilder);
    }

    internal class DateOnlyConverter : ValueConverter<DateOnly, string>
    {
        private const string format = "yyyy-MM-dd";
        public DateOnlyConverter() : base(d => d.ToString(format), d => DateOnly.ParseExact(d, format))
        {
        }
    }
}
