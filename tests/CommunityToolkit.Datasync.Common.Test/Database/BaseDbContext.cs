// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;
using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Common.Test.Database;

[ExcludeFromCodeCoverage]
public abstract class BaseDbContext<TContext, TEntity> : DbContext
    where TContext : DbContext
    where TEntity : class, IMovie, ITableData, new()
{
    protected BaseDbContext(DbContextOptions<TContext> options) : base(options)
    {
    }

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
    /// Populates the database with the core set of movies.  Ensures that we
    /// have the same data for all tests.
    /// </summary>
    protected void PopulateDatabase()
    {
        List<TEntity> movies = Movies.OfType<TEntity>().ToList();
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
}