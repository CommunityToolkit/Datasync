// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
public class PgEntityTableRepository_Tests : RepositoryTests<PgEntityMovie>
{
    #region Setup
    private readonly Random random = new();
    private readonly string connectionString;
    private readonly List<PgEntityMovie> movies;
    private readonly Lazy<PgDbContext> _context;

    public PgEntityTableRepository_Tests(ITestOutputHelper output) : base()
    {
        this.connectionString = Environment.GetEnvironmentVariable("DATASYNC_PGSQL_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            this._context = new Lazy<PgDbContext>(() => PgDbContext.CreateContext(this.connectionString, output));
            this.movies = Context.Movies.AsNoTracking().ToList();
        }
    }

    private PgDbContext Context { get => this._context.Value; }

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

    protected override Task<PgEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

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
