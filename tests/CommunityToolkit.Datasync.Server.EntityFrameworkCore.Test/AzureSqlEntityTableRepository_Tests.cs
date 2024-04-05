// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Database;
using CommunityToolkit.Datasync.Common.Test.Models;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
public class AzureSqlEntityTableRepository_Tests : RepositoryTests<EntityMovie>
{
    #region Setup
    private readonly Random random = new();
    private readonly string connectionString;
    private readonly List<EntityMovie> movies;
    private readonly Lazy<AzureSqlDbContext> _context;

    public AzureSqlEntityTableRepository_Tests(ITestOutputHelper output) : base()
    {
        this.connectionString = Environment.GetEnvironmentVariable("DATASYNC_AZSQL_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            this._context = new Lazy<AzureSqlDbContext>(() => AzureSqlDbContext.CreateContext(this.connectionString, output));
            this.movies = Context.Movies.AsNoTracking().ToList();
        }
    }

    private AzureSqlDbContext Context { get => this._context.Value; }

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

    protected override Task<EntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<EntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<EntityMovie>>(new EntityTableRepository<EntityMovie>(Context));

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
        Action act = () => _ = new EntityTableRepository<EntityMovie>(Context);
        act.Should().NotThrow();
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsConflictException_WhenDbConcurrencyUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<EntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<EntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        EntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateConcurrencyException("Concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409).And.WithPayload(expectedPayload);
    }

    [SkippableFact]
    public async Task WrapExceptionAsync_ThrowsRepositoryException_WhenDbUpdateExceptionThrown()
    {
        Skip.IfNot(CanRunLiveTests());
        EntityTableRepository<EntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<EntityMovie>;
        string id = await GetRandomEntityIdAsync(true);
        EntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateException("Non-concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        await act.Should().ThrowAsync<RepositoryException>();
    }
}