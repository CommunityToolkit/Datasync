// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class AzureSQL_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<AzureSqlEntityMovie>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private readonly string connectionString = Environment.GetEnvironmentVariable("DATASYNC_AZSQL_CONNECTIONSTRING");
    private List<AzureSqlEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            // Note: we don't clear entities on every run to speed up the test runs.  This can only be done because
            // the tests are read-only (associated with the query and get capabilities).  If the test being run writes
            // to the database then change clearEntities to true.
            output.WriteLine($"AzureSqlIsInitialized = {fixture.AzureSqlIsInitialized}");
            Context = await AzureSqlDbContext.CreateContextAsync(this.connectionString, output, clearEntities: !fixture.AzureSqlIsInitialized);
            this.movies = await Context.Movies.AsNoTracking().ToListAsync();
            fixture.AzureSqlIsInitialized = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private AzureSqlDbContext Context { get; set; }

    protected override string DriverName { get; } = "AzureSQL";

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

    protected override Task<AzureSqlEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<AzureSqlEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<AzureSqlEntityMovie>>(new EntityTableRepository<AzureSqlEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
