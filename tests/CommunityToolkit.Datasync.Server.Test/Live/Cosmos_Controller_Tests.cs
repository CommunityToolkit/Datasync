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
public class Cosmos_Controller_Tests : LiveControllerTests<CosmosEntityMovie>
{
    #region Setup
    private readonly DatabaseFixture _fixture;
    private readonly Random random = new();
    private readonly string connectionString;
    private readonly List<CosmosEntityMovie> movies;

    public Cosmos_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : base()
    {
        this._fixture = fixture;
        this.connectionString = Environment.GetEnvironmentVariable("DATASYNC_COSMOS_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            // Note: we don't clear entities on every run to speed up the test runs.  This can only be done because
            // the tests are read-only (associated with the query and get capabilities).  If the test being run writes
            // to the database then change clearEntities to true.
            output.WriteLine($"CosmosIsInitialized = {this._fixture.CosmosIsInitialized}");
            Context = CosmosDbContext.CreateContext(this.connectionString, output, clearEntities: !this._fixture.CosmosIsInitialized);
            this.movies = [.. Context.Movies.AsNoTracking()];
            this._fixture.CosmosIsInitialized = true;
        }
    }

    private CosmosDbContext Context { get; set; }

    protected override string DriverName { get; } = "Cosmos";

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

    protected override Task<CosmosEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<CosmosEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<CosmosEntityMovie>>(new EntityTableRepository<CosmosEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
