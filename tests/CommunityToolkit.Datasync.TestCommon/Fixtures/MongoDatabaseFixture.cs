// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Xunit;

namespace CommunityToolkit.Datasync.TestCommon.Fixtures;

[ExcludeFromCodeCoverage]
public class MongoDatabaseFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container;

    public MongoDatabaseFixture()
    {
        this._container = new MongoDbBuilder()
            .WithImage("mongo:latest")
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
