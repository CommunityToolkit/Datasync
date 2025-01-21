// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline.Helpers;

[ExcludeFromCodeCoverage]
public abstract class BaseTest
{
    /// <summary>
    /// The date/time of the start of the current test.
    /// </summary>
    protected DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a version of the TestDbContext backed by SQLite.
    /// </summary>
    protected static TestDbContext CreateContext(Action<DbContextOptionsBuilder<TestDbContext>> configureOptions = null)
    {
        SqliteConnection connection = CreateAndOpenConnection();
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection);
        configureOptions?.Invoke(optionsBuilder);
        TestDbContext context = new(optionsBuilder.Options) { Connection = connection };

        // Ensure the database is created.
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Creates a version of the TestDbContext backed by the specified SQLite connection.
    /// </summary>
    protected static TestDbContext CreateContext(SqliteConnection connection, Action<DbContextOptionsBuilder<TestDbContext>> configureOptions = null)
    {
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection);
        configureOptions?.Invoke(optionsBuilder);
        TestDbContext context = new(optionsBuilder.Options) { Connection = connection };

        // Ensure the database is created.
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Creates and opens an in-memory SQLite database connection.
    /// </summary>
    protected static SqliteConnection CreateAndOpenConnection()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates a response message based on code and content.
    /// </summary>
    protected static HttpResponseMessage GetResponse(string content, HttpStatusCode code)
    {
        HttpResponseMessage response = new(code)
        {
            Content = new StringContent(content, MediaTypeHeaderValue.Parse("application/json"))
        };
        return response;
    }

    /// <summary>
    /// Creates a page of items to use as a response.
    /// </summary>
    /// <param name="nItems"></param>
    /// <param name="totalCount"></param>
    /// <param name="nextLink"></param>
    /// <returns></returns>
    protected static Page<ClientMovie> CreatePage(int nItems, int? totalCount = null, string nextLink = null)
    {
        List<ClientMovie> items = [];
        for (int i = 0; i < nItems; i++)
        {
            ClientMovie item = new(TestData.Movies.BlackPanther)
            {
                Id = Guid.NewGuid().ToString("N"),
                UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1 * (i + 25)),
                Version = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            };
            items.Add(item);
        }

        return new Page<ClientMovie>()
        {
            Items = items,
            Count = totalCount,
            NextLink = nextLink
        };
    }
}
