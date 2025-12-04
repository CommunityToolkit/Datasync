// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;
using Xunit;

namespace CommunityToolkit.Datasync.TestCommon.Fixtures;

/// <summary>
/// A test fixture for implementing a PostgreSQL database using Testcontainers.
/// </summary>
[ExcludeFromCodeCoverage]
public class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlDatabaseFixture()
    {
        this._container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithCleanUp(true)
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .WithDatabase("testdb")
            .Build();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (this._container is not null)
        {
            await this._container.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await this._container.StartAsync();
        ConnectionString = this._container.GetConnectionString();
    }

    /// <summary>
    /// The connection string for the MySQL database.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;
}
