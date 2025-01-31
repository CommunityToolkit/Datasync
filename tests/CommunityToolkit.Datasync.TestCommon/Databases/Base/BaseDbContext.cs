// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public abstract class BaseDbContext<TContext, TEntity>(DbContextOptions<TContext> options) : DbContext(options)
    where TContext : DbContext
    where TEntity : class, IMovie, ITableData, new()
{

    /// <summary>
    /// If set, the <see cref="ITestOutputHelper"/> for logging.
    /// </summary>
    protected ITestOutputHelper OutputHelper { get; set; }

    /// <summary>
    /// The list of Movie IDs that are in the database.
    /// </summary>
    protected IList<string> MovieIds { get; set; }

    /// <summary>
    /// The collection of movies.
    /// </summary>
    public virtual DbSet<TEntity> Movies { get; set; }

    /// <summary>
    /// Executes the provided SQL statement for each entity in our set of entities.
    /// </summary>
    /// <param name="format"></param>
    protected void ExecuteRawSqlOnEachEntity(string format)
    {
        foreach (IEntityType table in Model.GetEntityTypes())
        {
            string sql = string.Format(format, table.GetTableName());
            Database.ExecuteSqlRaw(sql);
        }
    }

    /// <summary>
    /// Executes the provided SQL statement for each entity in our set of entities.
    /// </summary>
    /// <param name="format"></param>
    protected async Task ExecuteRawSqlOnEachEntityAsync(string format)
    {
        foreach (IEntityType table in Model.GetEntityTypes())
        {
            string sql = string.Format(format, table.GetTableName());
            await Database.ExecuteSqlRawAsync(sql);
        }
    }

    /// <summary>
    /// Populates the database with the core set of movies.  Ensures that we have the same data for all tests.
    /// </summary>
    [SuppressMessage("Performance", "CA1827:Do not use Count() or LongCount() when Any() can be used", Justification = "CosmosDB does not support .Any()")]
    protected void PopulateDatabase()
    {
        bool hasEntities = Movies.Count() > 0;
        if (hasEntities)
        {
            return;
        }

        List<TEntity> movies = [.. TestData.Movies.OfType<TEntity>()];
        MovieIds = movies.ConvertAll(m => m.Id);

        // Make sure we are populating with the right data
        bool setUpdatedAt = Attribute.IsDefined(typeof(TEntity).GetProperty("UpdatedAt")!, typeof(UpdatedByRepositoryAttribute));
        bool setVersion = Attribute.IsDefined(typeof(TEntity).GetProperty("Version")!, typeof(UpdatedByRepositoryAttribute));
        foreach (TEntity movie in movies)
        {
            if (setUpdatedAt)
            {
                movie.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (setVersion)
            {
                movie.Version = Guid.NewGuid().ToByteArray();
            }

            Movies.Add(movie);
        }

        SaveChanges();
    }

    protected async Task<int> PopulateDatabaseAsync()
    {
        int entityCount = await Movies.CountAsync();
        if (entityCount > 0)
        {
            return 0;
        }

        List<TEntity> movies = [.. TestData.Movies.OfType<TEntity>()];
        MovieIds = movies.ConvertAll(m => m.Id);

        // Make sure we are populating with the right data
        bool setUpdatedAt = Attribute.IsDefined(typeof(TEntity).GetProperty("UpdatedAt")!, typeof(UpdatedByRepositoryAttribute));
        bool setVersion = Attribute.IsDefined(typeof(TEntity).GetProperty("Version")!, typeof(UpdatedByRepositoryAttribute));
        foreach (TEntity movie in movies)
        {
            if (setUpdatedAt)
            {
                movie.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (setVersion)
            {
                movie.Version = Guid.NewGuid().ToByteArray();
            }

            Movies.Add(movie);
        }

        return await SaveChangesAsync();
    }
}