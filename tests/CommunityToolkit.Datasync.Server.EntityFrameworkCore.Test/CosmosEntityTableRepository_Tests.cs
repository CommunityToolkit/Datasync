// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
public class CosmosEntityTableRepository_Tests : RepositoryTests<CosmosEntityMovie>
{
    #region Setup
    private readonly Random random = new();
    private readonly string connectionString;
    private readonly List<CosmosEntityMovie> movies;
    private readonly Lazy<CosmosDbContext> _context;

    public CosmosEntityTableRepository_Tests(ITestOutputHelper output) : base()
    {
        this.connectionString = Environment.GetEnvironmentVariable("DATASYNC_COSMOS_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            this._context = new Lazy<CosmosDbContext>(() => CosmosDbContext.CreateContext(this.connectionString, output));
            this.movies = Context.Movies.AsNoTracking().ToList();
        }
    }

    private CosmosDbContext Context { get => this._context.Value; }

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