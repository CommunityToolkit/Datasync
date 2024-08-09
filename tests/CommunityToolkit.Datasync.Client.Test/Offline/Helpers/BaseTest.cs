// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Offline.Helpers;

[ExcludeFromCodeCoverage]
public abstract class BaseTest
{
    /// <summary>
    /// Creates a version of the TestDbContext backed by SQLite.
    /// </summary>
    /// <returns></returns>
    protected static TestDbContext CreateContext()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection);
        TestDbContext context = new(optionsBuilder.Options) { Connection = connection };
        return context;
    }
}
