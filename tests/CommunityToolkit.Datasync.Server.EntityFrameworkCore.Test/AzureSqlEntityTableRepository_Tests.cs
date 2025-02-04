// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

#pragma warning disable CS9113 // Parameter is unread.

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class AzureSqlEntityTableRepository_Tests(DatabaseFixture fixture, ITestOutputHelper output) : RepositoryTests<AzureSqlEntityMovie>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<AzureSqlEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(ConnectionStrings.AzureSql))
        {
            Context = await AzureSqlDbContext.CreateContextAsync(ConnectionStrings.AzureSql, output);
            this.movies = await Context.Movies.AsNoTracking().ToListAsync();
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

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(ConnectionStrings.AzureSql);

    protected override async Task<AzureSqlEntityMovie> GetEntityAsync(string id)
        => await Context.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

    protected override async Task<int> GetEntityCountAsync()
        => await Context.Movies.CountAsync();

    protected override Task<IRepository<AzureSqlEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<AzureSqlEntityMovie>>(new EntityTableRepository<AzureSqlEntityMovie>(Context));

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
        Action act = () => _ = new EntityTableRepository<AzureSqlEntityMovie>(Context);
        act.Should().NotThrow();
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsConflictException_WhenDbConcurrencyUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<AzureSqlEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<AzureSqlEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        AzureSqlEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateConcurrencyException("Concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409).And.WithPayload(expectedPayload);
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsRepositoryException_WhenDbUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<AzureSqlEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<AzureSqlEntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        AzureSqlEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateException("Non-concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        await act.Should().ThrowAsync<RepositoryException>();
    }
}