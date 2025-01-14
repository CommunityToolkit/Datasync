// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class CosmosDbContext(DbContextOptions<CosmosDbContext> options) : BaseDbContext<CosmosDbContext, CosmosEntityMovie>(options)
{
    public static async Task<CosmosDbContext> CreateContextAsync(string connectionString, ITestOutputHelper output = null)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        DbContextOptionsBuilder<CosmosDbContext> optionsBuilder = new DbContextOptionsBuilder<CosmosDbContext>()
            .UseCosmos(connectionString, databaseName: "unittests")
            .EnableLogging(output);
        CosmosDbContext context = new(optionsBuilder.Options);

        await context.InitializeDatabaseAsync();
        await context.PopulateDatabaseAsync();
        return context;
    }

    internal async Task InitializeDatabaseAsync()
    {
        List<CosmosEntityMovie> toDelete = await Movies.ToListAsync();
        RemoveRange(toDelete);
        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CosmosEntityMovie>(builder =>
        {
            builder.ToContainer("Movies");
            builder.HasNoDiscriminator();
            builder.HasPartitionKey(model => model.Id);
            builder.Property(model => model.EntityTag).IsETagConcurrency();
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
