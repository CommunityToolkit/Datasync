// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.MongoDB;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Fixtures;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class MongoDB_Controller_Tests(MongoDatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<MongoDBMovie>(), IClassFixture<MongoDatabaseFixture>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<MongoDBMovie> movies = [];

    public async Task InitializeAsync()
    {
        Context = await MongoDBContext.CreateContextAsync(fixture.ConnectionString, output);
        this.movies = await Context.Movies.Find(new BsonDocument()).ToListAsync();
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

    protected override bool CanRunLiveTests() => true;

    // We don't support querying date components in MongoDB because the underlying database doesn't support it.
    protected override bool CanQueryDateComponents() => false;

    // We don't support complex math (like floor() and ceiling()) in Mongo because the underlying database doesn't support it.
    protected override bool CanDoComplexMath() => false;

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
