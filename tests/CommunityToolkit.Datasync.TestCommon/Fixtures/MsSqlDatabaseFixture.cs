// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Testcontainers.MsSql;
using Xunit;

namespace CommunityToolkit.Datasync.TestCommon.Fixtures;

[ExcludeFromCodeCoverage]
public class MsSqlDatabaseFixture : IAsyncLifetime
{
    private const string imageName = "mcr.microsoft.com/mssql/server:2025-CU2-ubuntu-22.04";
    private readonly MsSqlContainer _container;

    public MsSqlDatabaseFixture()
    {
        this._container = new MsSqlBuilder(imageName).Build();
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
