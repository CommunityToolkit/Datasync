// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.MongoDB.Test;

[ExcludeFromCodeCoverage]
public class MongoDBRepository_Tests(ITestOutputHelper output) : RepositoryTests<MongoDBMovie>(), IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<MongoDBMovie> movies = [];

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(ConnectionStrings.MongoCommunity))
        {
            Context = await MongoDBContext.CreateContextAsync(ConnectionStrings.MongoCommunity, output);
            this.movies = await Context.Movies.Find(new BsonDocument()).ToListAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    public MongoDBContext Context { get; set; }

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(ConnectionStrings.CosmosDb);

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