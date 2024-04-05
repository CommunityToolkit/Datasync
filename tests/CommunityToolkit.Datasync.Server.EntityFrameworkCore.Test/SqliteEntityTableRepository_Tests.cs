// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Database;
using CommunityToolkit.Datasync.Common.Test.Models.Movie;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
public class SqliteEntityTableRepository_Tests : RepositoryTests<SqliteEntityMovie>
{
    #region Setup
    private readonly Random random = new();
    private readonly List<SqliteEntityMovie> movies;
    private readonly Lazy<SqliteDbContext> lazyContext;

    public SqliteEntityTableRepository_Tests(ITestOutputHelper output)
    {
        this.lazyContext = new(() => SqliteDbContext.CreateContext(output));
        this.movies = Context.Movies.AsNoTracking().ToList();
    }

    private SqliteDbContext Context { get => this.lazyContext.Value; }

    protected override Task<SqliteEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<SqliteEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<SqliteEntityMovie>>(new EntityTableRepository<SqliteEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(Context.Movies.Count())].Id : Guid.NewGuid().ToString());
    #endregion

    [Fact]
    public void EntityTableRepository_BadDbSet_Throws()
    {
        Action act = () => _ = new EntityTableRepository<EntityTableData>(Context);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EntityTableRepository_GoodDbSet_Works()
    {
        Action act = () => _ = new EntityTableRepository<SqliteEntityMovie>(Context);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("id-001")]
    public async Task WrapExceptionAsync_ThrowsConflictException_WhenDbConcurrencyUpdateExceptionThrown(string id)
    {
        EntityTableRepository<SqliteEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<SqliteEntityMovie>;
        SqliteEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateConcurrencyException("Concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409).And.WithPayload(expectedPayload);
    }

    [Theory]
    [InlineData("id-001")]
    public async Task WrapExceptionAsync_ThrowsRepositoryException_WhenDbUpdateExceptionThrown(string id)
    {
        EntityTableRepository<SqliteEntityMovie> repository = await GetPopulatedRepositoryAsync() as EntityTableRepository<SqliteEntityMovie>;
        SqliteEntityMovie expectedPayload = await GetEntityAsync(id);

        static Task innerAction() => throw new DbUpdateException("Non-concurrency exception");

        Func<Task> act = async () => await repository.WrapExceptionAsync(id, innerAction);
        await act.Should().ThrowAsync<RepositoryException>();
    }
}
