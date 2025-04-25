// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class MySQL_Controller_Tests(MySqlDatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<MysqlEntityMovie>, IClassFixture<MySqlDatabaseFixture>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<MysqlEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        Context = await MysqlDbContext.CreateContextAsync(fixture.ConnectionString, output);
        this.movies = await Context.Movies.AsNoTracking().ToListAsync();
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private MysqlDbContext Context { get; set; }

    protected override string DriverName { get; } = "MySQL";

    protected override bool CanRunLiveTests() => true;

    protected override async Task<MysqlEntityMovie> GetEntityAsync(string id)
        => await Context.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

    protected override async Task<int> GetEntityCountAsync()
        => await Context.Movies.CountAsync();

    protected override Task<IRepository<MysqlEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<MysqlEntityMovie>>(new EntityTableRepository<MysqlEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
