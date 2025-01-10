// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

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
public class CosmosEntityTableRepository_Tests(DatabaseFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private string connectionString = string.Empty;
    private List<CosmosEntityMovie> movies;

    /// <summary>
    /// The time that the current test started.
    /// </summary>
    protected DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

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

    protected bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

    protected Task<CosmosEntityMovie> GetEntityAsync(string id) 
        => Context.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

    protected Task<int> GetEntityCountAsync()
        => Context.Movies.CountAsync();

    protected Task<IRepository<CosmosEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<CosmosEntityMovie>>(new EntityTableRepository<CosmosEntityMovie>(Context));

    protected Task<string> GetRandomEntityIdAsync(bool exists)
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

    /// <summary>
    /// The tests below this line are direct copies of the RepositoryTests tests, but
    /// specifically modified for Cosmos DB.
    /// </summary>
    /// <returns></returns>
    #region Repository Tests
    #region AsQueryableAsync
    [SkippableFact]
    public async Task AsQueryableAsync_ReturnsQueryable()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        IQueryable<CosmosEntityMovie> sut = await Repository.AsQueryableAsync();

        sut.Should().NotBeNull().And.BeAssignableTo<IQueryable<CosmosEntityMovie>>();
    }

    [SkippableFact]
    public async Task AsQueryableAsync_CanRetrieveSingleItems()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expected = await GetEntityAsync(id);
        IQueryable<CosmosEntityMovie> queryable = await Repository.AsQueryableAsync();
        CosmosEntityMovie actual = queryable.Single(m => m.Id == id);

        actual.Should().BeEquivalentTo(expected);
    }

    [SkippableFact]
    public async Task AsQueryableAsync_CanRetrieveFilteredLists()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        int expected = TestData.Movies.Count<CosmosEntityMovie>(m => m.Rating == MovieRating.R);
        IQueryable<CosmosEntityMovie> queryable = await Repository.AsQueryableAsync();
        List<CosmosEntityMovie> actual = await queryable.Where(m => m.Rating == MovieRating.R).ToListAsync();

        actual.Should().HaveCount(expected);
    }

    [SkippableFact]
    public async Task AsQueryableAsync_CanSelectFromList()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        int expected = TestData.Movies.Count<CosmosEntityMovie>(m => m.Rating == MovieRating.R);
        IQueryable<CosmosEntityMovie> queryable = await Repository.AsQueryableAsync();
        var actual = queryable.Where(m => m.Rating == MovieRating.R).Select(m => new { m.Id, m.Title }).ToList();

        actual.Should().HaveCount(expected);
    }

    [SkippableFact]
    public async Task AsQueryableAsync_CanUseTopAndSkip()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        IQueryable<CosmosEntityMovie> queryable = await Repository.AsQueryableAsync();
        List<CosmosEntityMovie> actual = await queryable.Where(m => m.Rating == MovieRating.R).Skip(5).Take(20).ToListAsync();

        actual.Should().HaveCount(20);
    }

    /// <summary>
    /// This test simulates a paged response from the client for a datasync operation.
    /// </summary>
    [SkippableFact]
    public async Task AsQueryableAsync_CanRetrievePagedDatasyncQuery()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        IQueryable<CosmosEntityMovie> queryable = await Repository.AsQueryableAsync();
        List<CosmosEntityMovie> actual = queryable.Where(m => m.UpdatedAt > DateTimeOffset.UnixEpoch && !m.Deleted).OrderBy(m => m.UpdatedAt).Skip(10).Take(10).ToList();

        actual.Should().HaveCount(10);
    }
    #endregion

    #region CreateAsync
    [SkippableFact]
    public async Task CreateAsync_CreatesNewEntity_WithSpecifiedId()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        CosmosEntityMovie addition = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        CosmosEntityMovie sut = addition.Clone();
        await Repository.CreateAsync(sut);
        CosmosEntityMovie actual = await GetEntityAsync(id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.Should().NotBeEquivalentTo<ITableData>(addition).And.HaveEquivalentMetadataTo(sut);
        actual.Id.Should().Be(id);
        actual.UpdatedAt.Should().BeAfter(StartTime);
        actual.Version.Should().NotBeNullOrEmpty();
    }

    [SkippableTheory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateAsync_CreatesNewEntity_WithNullId(string id)
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        CosmosEntityMovie addition = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther);
        addition.Id = id;
        CosmosEntityMovie sut = addition.Clone();
        await Repository.CreateAsync(sut);
        CosmosEntityMovie actual = await GetEntityAsync(sut.Id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.UpdatedAt.Should().BeAfter(StartTime);
    }

    [SkippableFact]
    public async Task CreateAsync_ThrowsConflict()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie addition = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        CosmosEntityMovie sut = addition.Clone();
        CosmosEntityMovie expected = await GetEntityAsync(id);
        Func<Task> act = async () => await Repository.CreateAsync(sut);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409).And.WithPayload(expected);
    }

    [SkippableFact]
    public async Task CreateAsync_UpdatesMetadata()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        CosmosEntityMovie addition = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        CosmosEntityMovie sut = addition.Clone();
        sut.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        byte[] expectedVersion = [ 0x01, 0x02, 0x03, 0x04 ];
        sut.Version = [.. expectedVersion];
        await Repository.CreateAsync(sut);
        CosmosEntityMovie actual = await GetEntityAsync(id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.Should().NotBeEquivalentTo<ITableData>(addition).And.HaveEquivalentMetadataTo(sut);
        actual.Id.Should().Be(id);
        actual.UpdatedAt.Should().BeAfter(StartTime);
        actual.Version.Should().NotBeEquivalentTo(expectedVersion);
    }

    [SkippableFact]
    public async Task CreateAsync_StoresDisconnectedEntity()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        CosmosEntityMovie addition = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        CosmosEntityMovie sut = addition.Clone();
        sut.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        byte[] expectedVersion = [0x01, 0x02, 0x03, 0x04];
        sut.Version = [.. expectedVersion];
        await Repository.CreateAsync(sut);
        CosmosEntityMovie actual = await GetEntityAsync(id);

        actual.Should().NotBeSameAs(sut);
    }
    #endregion

    #region DeleteAsync
    [SkippableTheory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DeleteAsync_Throws_OnBadIds(string id)
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        Func<Task> act = async () => await Repository.DeleteAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
        (await GetEntityCountAsync()).Should().Be(TestData.Movies.Count<CosmosEntityMovie>());
    }

    [SkippableFact]
    public async Task DeleteAsync_Throws_OnMissingIds()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        Func<Task> act = async () => await Repository.DeleteAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(404);
        (await GetEntityCountAsync()).Should().Be(TestData.Movies.Count<CosmosEntityMovie>());
    }

    [SkippableFact]
    public async Task DeleteAsync_Throws_WhenVersionMismatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expected = await GetEntityAsync(id);
        byte[] version = Guid.NewGuid().ToByteArray();
        Func<Task> act = async () => await Repository.DeleteAsync(id, version);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(412).And.WithPayload(expected);
    }

    [SkippableFact]
    public async Task DeleteAsync_Deletes_WhenVersionMatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expected = await GetEntityAsync(id);
        byte[] version = expected.Version.ToArray();
        await Repository.DeleteAsync(id, version);
        CosmosEntityMovie actual = await GetEntityAsync(id);

        actual.Should().BeNull();
    }

    [SkippableFact]
    public async Task DeleteAsync_Deletes_WhenNoVersion()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        await Repository.DeleteAsync(id);
        CosmosEntityMovie actual = await GetEntityAsync(id);

        actual.Should().BeNull();
    }
    #endregion

    #region ReadAsync
    [SkippableFact]
    public async Task ReadAsync_ReturnsDisconnectedEntity()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie expected = await GetEntityAsync(id);
        CosmosEntityMovie actual = await Repository.ReadAsync(id);

        actual.Should().BeEquivalentTo(expected).And.NotBeSameAs(expected);
    }

    [SkippableTheory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ReadAsync_Throws_OnBadId(string id)
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        Func<Task> act = async () => _ = await Repository.ReadAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
    }

    [SkippableFact]
    public async Task ReadAsync_Throws_OnMissingId()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        Func<Task> act = async () => _ = await Repository.ReadAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(404);
    }
    #endregion

    #region ReplaceAsync
    [SkippableTheory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ReplaceAsync_Throws_OnBadId(string id)
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        CosmosEntityMovie replacement = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther);
        replacement.Id = id;
        Func<Task> act = async () => await Repository.ReplaceAsync(replacement);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Throws_OnMissingId()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        CosmosEntityMovie replacement = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        Func<Task> act = async () => await Repository.ReplaceAsync(replacement);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(404);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Throws_OnVersionMismatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie replacement = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        CosmosEntityMovie expected = await GetEntityAsync(id);
        byte[] version = Guid.NewGuid().ToByteArray();
        Func<Task> act = async () => await Repository.ReplaceAsync(replacement, version);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(412).And.WithPayload(expected);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Replaces_OnVersionMatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie replacement = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        CosmosEntityMovie expected = await GetEntityAsync(id);
        byte[] version = expected.Version.ToArray();
        await Repository.ReplaceAsync(replacement, version);
        CosmosEntityMovie actual = await GetEntityAsync(id);

        actual.Should().BeEquivalentTo<IMovie>(replacement).And.NotBeEquivalentTo<ITableData>(expected);
        actual.Version.Should().NotBeEquivalentTo(version);
        actual.UpdatedAt.Should().BeAfter(StartTime);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Replaces_OnNoVersion()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosEntityMovie> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        CosmosEntityMovie replacement = TestData.Movies.OfType<CosmosEntityMovie>(TestData.Movies.BlackPanther, id);
        CosmosEntityMovie expected = await GetEntityAsync(id);
        byte[] version = expected.Version.ToArray();
        await Repository.ReplaceAsync(replacement);
        CosmosEntityMovie actual = await GetEntityAsync(id);

        actual.Should().BeEquivalentTo<IMovie>(replacement).And.NotBeEquivalentTo<ITableData>(expected);
        actual.Version.Should().NotBeEquivalentTo(version);
        actual.UpdatedAt.Should().BeAfter(StartTime);
    }
    #endregion
    #endregion
}