// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.MongoDB;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class CosmosMongo_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<MongoDBMovie>(), IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<MongoDBMovie> movies = [];

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(ConnectionStrings.CosmosMongo))
        {
            // Note: we don't clear entities on every run to speed up the test runs.  This can only be done because
            // the tests are read-only (associated with the query and get capabilities).  If the test being run writes
            // to the database then change clearEntities to true.
            output.WriteLine($"CosmosMongoIsInitialized = {fixture.CosmosMongoIsInitialized}");
            Context = await MongoDBContext.CreateContextAsync(ConnectionStrings.CosmosMongo, output, clearEntities: !fixture.CosmosMongoIsInitialized);
            this.movies = await Context.Movies.Find(new BsonDocument()).ToListAsync();
            fixture.CosmosMongoIsInitialized = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private MongoDBContext Context { get; set; }

    protected override string DriverName { get; } = "MongoDB";

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(ConnectionStrings.CosmosMongo);

    protected override async Task<MongoDBMovie> GetEntityAsync(string id)
        => await Context.Movies.Find(Builders<MongoDBMovie>.Filter.Eq(x => x.Id, id)).FirstOrDefaultAsync();

    protected override async Task<int> GetEntityCountAsync()
        => (int)(await Context.Movies.CountDocumentsAsync(Builders<MongoDBMovie>.Filter.Empty));

    protected override Task<IRepository<MongoDBMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<MongoDBMovie>>(new MongoDBRepository<MongoDBMovie>(Context.Movies));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
