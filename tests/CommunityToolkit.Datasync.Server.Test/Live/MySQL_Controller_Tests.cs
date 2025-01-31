// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class MySQL_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<MysqlEntityMovie>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private readonly string connectionString = Environment.GetEnvironmentVariable("DATASYNC_MYSQL_CONNECTIONSTRING");
    private List<MysqlEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            // Note: we don't clear entities on every run to speed up the test runs.  This can only be done because
            // the tests are read-only (associated with the query and get capabilities).  If the test being run writes
            // to the database then change clearEntities to true.
            output.WriteLine($"MysqlIsInitialized = {fixture.MysqlIsInitialized}");
            Context = await MysqlDbContext.CreateContextAsync(this.connectionString, output, clearEntities: !fixture.MysqlIsInitialized);
            this.movies = await Context.Movies.AsNoTracking().ToListAsync();
            fixture.MysqlIsInitialized = true;
        }
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

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

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
