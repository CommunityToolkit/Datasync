// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Common.Test;

/// <summary>
/// A set of extension methods to support the Datasync Toolkit tests.
/// </summary>
[ExcludeFromCodeCoverage]
public static class LibraryExtensions
{
    private static readonly string[] categories = new string[] { "Microsoft.EntityFrameworkCore.Database.Command" };

    /// <summary>
    /// Enables the correct logging on a database context.
    /// </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <param name="current">The current database context.</param>
    /// <param name="output">The logging output helper.</param>
    /// <returns>The database context (for chaining).</returns>
    public static DbContextOptionsBuilder<TContext> EnableLogging<TContext>(this DbContextOptionsBuilder<TContext> current, ITestOutputHelper output) where TContext : DbContext
    {
        bool enableLogging = (Environment.GetEnvironmentVariable("ENABLE_SQL_LOGGING") ?? "false") == "true";
        if (output != null && enableLogging)
        {
            current
                .UseLoggerFactory(new TestLoggerFactory(output, categories))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();
        }

        return current;
    }

    /// <summary>
    /// Creates a copy of an <see cref="ITableData"/> into a base table data object.
    /// </summary>
    /// <typeparam name="T">The type of base table data object to create.</typeparam>
    /// <param name="entity">The entity to copy.</param>
    /// <returns>A copy of the original entity.</returns>
    public static T ToTableEntity<T>(this ITableData entity) where T : ITableData, new()
        => new()
        {
            Id = entity.Id,
            Deleted = entity.Deleted,
            UpdatedAt = entity.UpdatedAt,
            Version = (byte[])entity.Version.Clone()
        };

    /// <summary>
    /// Creates a deep clone of an entity using JSON serialization.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to clone.</typeparam>
    /// <param name="entity">The entity to clone.</param>
    /// <returns>The cloned entity.</returns>
    public static TEntity Clone<TEntity>(this TEntity entity)
    {
        JsonSerializerOptions options = new DatasyncServiceOptions().JsonSerializerOptions;
        string json = JsonSerializer.Serialize(entity, options);
        return JsonSerializer.Deserialize<TEntity>(json, options);
    }
}
