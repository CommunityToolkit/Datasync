// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class Cosmos_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<CosmosEntityMovie>(), IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<CosmosEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(ConnectionStrings.CosmosDb))
        {
            // Note: we don't clear entities on every run to speed up the test runs.  This can only be done because
            // the tests are read-only (associated with the query and get capabilities).  If the test being run writes
            // to the database then change clearEntities to true.
            output.WriteLine($"CosmosIsInitialized = {fixture.CosmosIsInitialized}");
            Context = await CosmosDbContext.CreateContextAsync(ConnectionStrings.CosmosDb, output, clearEntities: !fixture.CosmosIsInitialized);
            this.movies = await Context.Movies.AsNoTracking().ToListAsync();
            fixture.CosmosIsInitialized = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private CosmosDbContext Context { get; set; }

    protected override string DriverName { get; } = "Cosmos";

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(ConnectionStrings.CosmosDb);

    protected override async Task<CosmosEntityMovie> GetEntityAsync(string id)
        => await Context.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

    protected override async Task<int> GetEntityCountAsync()
        => await Context.Movies.CountAsync();

    protected override Task<IRepository<CosmosEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<CosmosEntityMovie>>(new EntityTableRepository<CosmosEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion

    /// <summary>
    /// We test the 400 Bad Request client-side evaluation error here because Cosmos has more restrictions than most,
    /// so it's easier to test the code path.
    /// </summary>
    [SkippableFact]
    public async Task ClientSideEvaluation_Produces_400BadRequest()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> repository = await GetPopulatedRepositoryAsync();
        TableController<CosmosEntityMovie> tableController = new(repository);
        tableController.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Get, $"{MovieEndpoint}?$filter=((year div 1000.5) eq 2)");

        Func<Task> act = async () => _ = await tableController.QueryAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
    }
}
