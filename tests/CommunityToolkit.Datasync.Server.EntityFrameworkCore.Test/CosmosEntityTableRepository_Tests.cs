// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS9113 // Parameter is unread.

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class CosmosEntityTableRepository_Tests(DatabaseFixture fixture, ITestOutputHelper output) : RepositoryTests<CosmosEntityMovie>(), IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<CosmosEntityMovie> movies = [];

    public async ValueTask InitializeAsync()
    {
        if (!string.IsNullOrEmpty(ConnectionStrings.CosmosDb))
        {
            Context = await CosmosDbContext.CreateContextAsync(ConnectionStrings.CosmosDb, output);
            this.movies = await Context.Movies.AsNoTracking().ToListAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private CosmosDbContext Context { get; set; }

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

    [Fact]
    public void EntityTableRepository_BadDbSet_Throws()
    {
        Assert.SkipUnless(CanRunLiveTests(), "Live tests are not enabled.");
        Action act = () => _ = new EntityTableRepository<EntityTableData>(Context);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EntityTableRepository_GoodDbSet_Works()
    {
        Assert.SkipUnless(CanRunLiveTests(), "Live tests are not enabled.");
        Action act = () => _ = new EntityTableRepository<CosmosEntityMovie>(Context);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task WrapExceptionAsync_ThrowsConflictException_WhenDbConcurrencyUpdateExceptionThrown()
    {
        Assert.SkipUnless(CanRunLiveTests(), "Live tests are not enabled.");
        EntityTableRepository<CosmosEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<CosmosEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateConcurrencyException("Concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409).And.WithPayload(expectedPayload);
    }

    [Fact]
    public async Task WrapExceptionAsync_ThrowsRepositoryException_WhenDbUpdateExceptionThrown()
    {
        Assert.SkipUnless(CanRunLiveTests(), "Live tests are not enabled.");
        EntityTableRepository<CosmosEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<CosmosEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateException("Non-concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        await act.Should().ThrowAsync<RepositoryException>();
    }
}