// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

#pragma warning disable CS9113 // Parameter is unread.

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

/// <summary>
/// Note that this is a completely different set of tests than the RepositoryTests because CosmosDB 
/// EF Core driver is async only - there is no sync version of the driver in EFCore 9.0.  They basically
/// broke the driver to enforce async behavior.
/// </summary>
/// <param name="fixture"></param>
/// <param name="output"></param>
[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class CosmosEntityTableRepository_Tests(DatabaseFixture fixture, ITestOutputHelper output) : RepositoryTests<CosmosEntityMovie>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private string connectionString = string.Empty;
    private List<CosmosEntityMovie> movies;

    public async Task InitializeAsync()
    {
        this.connectionString = Environment.GetEnvironmentVariable("DATASYNC_COSMOS_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            Context = await CosmosDbContext.CreateContextAsync(this.connectionString, output);
            this.movies = await Context.Movies.AsNoTracking().ToListAsync();
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private CosmosDbContext Context { get; set; }

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

    protected override Task<CosmosEntityMovie> GetEntityAsync(string id) 
        => Context.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

    protected override Task<int> GetEntityCountAsync()
        => Context.Movies.CountAsync();

    protected override Task<IRepository<CosmosEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<CosmosEntityMovie>>(new EntityTableRepository<CosmosEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion

    [SkippableFact]
    public void EntityTableRepository_BadDbSet_Throws()
    {
        Skip.IfNot(CanRunLiveTests());
        Action act = () => _ = new EntityTableRepository<EntityTableData>(Context);
        act.Should().Throw<ArgumentException>();
    }

    [SkippableFact]
    public void EntityTableRepository_GoodDbSet_Works()
    {
        Skip.IfNot(CanRunLiveTests());
        Action act = () => _ = new EntityTableRepository<CosmosEntityMovie>(Context);
        act.Should().NotThrow();
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsConflictException_WhenDbConcurrencyUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<CosmosEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<CosmosEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateConcurrencyException("Concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409).And.WithPayload(expectedPayload);
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsRepositoryException_WhenDbUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<CosmosEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<CosmosEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateException("Non-concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        await act.Should().ThrowAsync<RepositoryException>();
    }
}