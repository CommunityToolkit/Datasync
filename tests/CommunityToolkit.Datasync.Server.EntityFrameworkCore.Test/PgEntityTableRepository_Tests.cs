// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

#pragma warning disable CS9113 // Parameter is unread.

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class PgEntityTableRepository_Tests(PostgreSqlDatabaseFixture fixture, ITestOutputHelper output) : RepositoryTests<PgEntityMovie>, IClassFixture<PostgreSqlDatabaseFixture>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<PgEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        Context = await PgDbContext.CreateContextAsync(fixture.ConnectionString, output);
        this.movies = await Context.Movies.AsNoTracking().ToListAsync();
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private PgDbContext Context { get; set; }

    protected override bool CanRunLiveTests() => true;

    protected override async Task<PgEntityMovie> GetEntityAsync(string id)
        => await Context.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

    protected override async Task<int> GetEntityCountAsync()
        => await Context.Movies.CountAsync();

    protected override Task<IRepository<PgEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<PgEntityMovie>>(new EntityTableRepository<PgEntityMovie>(Context));

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
        Action act = () => _ = new EntityTableRepository<PgEntityMovie>(Context);
        act.Should().NotThrow();
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsConflictException_WhenDbConcurrencyUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<PgEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<PgEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        PgEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateConcurrencyException("Concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409).And.WithPayload(expectedPayload);
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsRepositoryException_WhenDbUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<PgEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<PgEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        PgEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateException("Non-concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        await act.Should().ThrowAsync<RepositoryException>();
    }
}
