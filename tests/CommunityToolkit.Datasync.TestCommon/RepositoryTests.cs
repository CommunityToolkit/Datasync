// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0300, IDE0305 // Collection initialization can be simplified, Use LINQ to replace loops

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.TestCommon.Models;
using AwesomeAssertions;
using Xunit;

namespace CommunityToolkit.Datasync.TestCommon;

/// <summary>
/// A set of common tests across all repository implementations.
/// </summary>
/// <typeparam name="TEntity">the type of entity under test.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class RepositoryTests<TEntity> where TEntity : class, ITableData, IMovie, new()
{
    /// <summary>
    /// The time that the current test started.
    /// </summary>
    protected DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns true if all the requirements for live tests are met.
    /// </summary>
    protected virtual bool CanRunLiveTests() => true;

    /// <summary>
    /// The actual test class must provide an implementation that retrieves the entity through
    /// the backing data store.
    /// </summary>
    /// <param name="id">The ID of the entity to retrieve.</param>
    /// <returns>Either <c>null</c> if the entity does not exist, or the entity.</returns>
    protected abstract Task<TEntity> GetEntityAsync(string id);

    /// <summary>
    /// The actual test class must provide an implementation that retrieves the entity count in
    /// the backing data store.
    /// </summary>
    /// <returns>The number of entities in the store.</returns>
    protected abstract Task<int> GetEntityCountAsync();

    /// <summary>
    /// Retrieves a populated repository for testing.
    /// </summary>
    protected abstract Task<IRepository<TEntity>> GetPopulatedRepositoryAsync();

    /// <summary>
    /// Retrieves a random ID from the database for testing.
    /// </summary>
    protected abstract Task<string> GetRandomEntityIdAsync(bool exists);

    #region AsQueryableAsync
    [SkippableFact]
    public async Task AsQueryableAsync_ReturnsQueryable()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        IQueryable<TEntity> sut = await Repository.AsQueryableAsync();

        sut.Should().NotBeNull().And.BeAssignableTo<IQueryable<TEntity>>();
    }

    [SkippableFact]
    public async Task AsQueryableAsync_CanRetrieveFilteredLists()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        int expected = TestData.Movies.Count<TEntity>(m => m.Rating == MovieRating.R);
        IQueryable<TEntity> queryable = await Repository.AsQueryableAsync();
        IQueryable<TEntity> sut = queryable
            .Where(m => m.Rating == MovieRating.R);
        IList<TEntity> actual = await Repository.ToListAsync(sut);

        actual.Should().HaveCount(expected);
    }

    [SkippableFact]
    public async Task AsQueryableAsync_CanRetrieveOrderedLists()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        int expected = TestData.Movies.Count<TEntity>();
        IQueryable<TEntity> queryable = await Repository.AsQueryableAsync();

        // We pick this set of orderings because we create a CosmosDB composite index for these already.
        IQueryable<TEntity> sut = queryable
            .OrderBy(m => m.ReleaseDate)
            .ThenBy(m => m.Id);

        IList<TEntity> actual = await Repository.ToListAsync(sut);

        actual.Should().HaveCount(expected);
    }

    [SkippableFact]
    public async Task AsQueryableAsync_CanUseTopAndSkip()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        IQueryable<TEntity> queryable = await Repository.AsQueryableAsync();
        IQueryable<TEntity> sut = queryable
            .Where(m => m.Rating == MovieRating.R)
            .Skip(5)
            .Take(20);
        IList<TEntity> actual = await Repository.ToListAsync(sut);

        actual.Should().HaveCount(20);
    }

    /// <summary>
    /// This test simulates a paged response from the client for a datasync operation.
    /// </summary>
    [SkippableFact]
    public async Task AsQueryableAsync_CanRetrievePagedDatasyncQuery()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        IQueryable<TEntity> queryable = await Repository.AsQueryableAsync();
        IQueryable<TEntity> sut = queryable
            .Where(m => m.UpdatedAt > DateTimeOffset.UnixEpoch && !m.Deleted)
            .OrderBy(m => m.UpdatedAt)
            .Skip(10)
            .Take(10);
        IList<TEntity> actual = await Repository.ToListAsync(sut);

        actual.Should().HaveCount(10);
    }
    #endregion

    #region CreateAsync
    [SkippableFact]
    public async Task CreateAsync_CreatesNewEntity_WithSpecifiedId()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        TEntity addition = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        TEntity sut = addition.Clone();
        await Repository.CreateAsync(sut);
        TEntity actual = await GetEntityAsync(id);

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

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        TEntity addition = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther);
        addition.Id = id;
        TEntity sut = addition.Clone();
        await Repository.CreateAsync(sut);
        TEntity actual = await GetEntityAsync(sut.Id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.UpdatedAt.Should().BeAfter(StartTime);
    }

    [SkippableFact]
    public async Task CreateAsync_ThrowsConflict()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        TEntity addition = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        TEntity sut = addition.Clone();
        TEntity expected = await GetEntityAsync(id);
        Func<Task> act = async () => await Repository.CreateAsync(sut);

        HttpException ex = (await act.Should().ThrowAsync<HttpException>()).Subject.First();
        ex.StatusCode.Should().Be(409);
        ex.Payload.Should().BeEquivalentTo<IMovie>(expected).And.HaveEquivalentMetadataTo(expected);
    }

    [SkippableFact]
    public async Task CreateAsync_UpdatesMetadata()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        TEntity addition = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        TEntity sut = addition.Clone();
        sut.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        byte[] expectedVersion = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        sut.Version = expectedVersion.ToArray();
        await Repository.CreateAsync(sut);
        TEntity actual = await GetEntityAsync(id);

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

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        TEntity addition = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        TEntity sut = addition.Clone();
        sut.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        byte[] expectedVersion = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        sut.Version = expectedVersion.ToArray();
        await Repository.CreateAsync(sut);
        TEntity actual = await GetEntityAsync(id);

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

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        Func<Task> act = async () => await Repository.DeleteAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
        (await GetEntityCountAsync()).Should().Be(TestData.Movies.Count<TEntity>());
    }

    [SkippableFact]
    public async Task DeleteAsync_Throws_OnMissingIds()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        Func<Task> act = async () => await Repository.DeleteAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(404);
        (await GetEntityCountAsync()).Should().Be(TestData.Movies.Count<TEntity>());
    }

    [SkippableFact]
    public async Task DeleteAsync_Throws_WhenVersionMismatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        TEntity expected = await GetEntityAsync(id);
        byte[] version = Guid.NewGuid().ToByteArray();
        Func<Task> act = async () => await Repository.DeleteAsync(id, version);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(412).And.WithPayload(expected);
    }

    [SkippableFact]
    public async Task DeleteAsync_Deletes_WhenVersionMatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        TEntity expected = await GetEntityAsync(id);
        byte[] version = expected.Version.ToArray();
        await Repository.DeleteAsync(id, version);
        TEntity actual = await GetEntityAsync(id);

        actual.Should().BeNull();
    }

    [SkippableFact]
    public async Task DeleteAsync_Deletes_WhenNoVersion()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        await Repository.DeleteAsync(id);
        TEntity actual = await GetEntityAsync(id);

        actual.Should().BeNull();
    }
    #endregion

    #region ReadAsync
    [SkippableFact]
    public async Task ReadAsync_ReturnsDisconnectedEntity()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        TEntity expected = await GetEntityAsync(id);
        TEntity actual = await Repository.ReadAsync(id);

        actual.Should().BeEquivalentTo(expected).And.NotBeSameAs(expected);
    }

    [SkippableTheory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ReadAsync_Throws_OnBadId(string id)
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        Func<Task> act = async () => _ = await Repository.ReadAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
    }

    [SkippableFact]
    public async Task ReadAsync_Throws_OnMissingId()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
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

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        TEntity replacement = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther);
        replacement.Id = id;
        Func<Task> act = async () => await Repository.ReplaceAsync(replacement);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Throws_OnMissingId()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(false);
        TEntity replacement = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        Func<Task> act = async () => await Repository.ReplaceAsync(replacement);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(404);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Throws_OnVersionMismatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        TEntity replacement = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        TEntity expected = await GetEntityAsync(id);
        byte[] version = Guid.NewGuid().ToByteArray();
        Func<Task> act = async () => await Repository.ReplaceAsync(replacement, version);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(412).And.WithPayload(expected);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Replaces_OnVersionMatch()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        TEntity replacement = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        TEntity expected = await GetEntityAsync(id);
        byte[] version = expected.Version.ToArray();
        await Repository.ReplaceAsync(replacement, version);
        TEntity actual = await GetEntityAsync(id);

        actual.Should().BeEquivalentTo<IMovie>(replacement).And.NotBeEquivalentTo<ITableData>(expected);
        actual.Version.Should().NotBeEquivalentTo(version);
        actual.UpdatedAt.Should().BeAfter(StartTime);
    }

    [SkippableFact]
    public async Task ReplaceAsync_Replaces_OnNoVersion()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<TEntity> Repository = await GetPopulatedRepositoryAsync();
        string id = await GetRandomEntityIdAsync(true);
        TEntity replacement = TestData.Movies.OfType<TEntity>(TestData.Movies.BlackPanther, id);
        TEntity expected = await GetEntityAsync(id);
        byte[] version = expected.Version.ToArray();
        await Repository.ReplaceAsync(replacement);
        TEntity actual = await GetEntityAsync(id);

        actual.Should().BeEquivalentTo<IMovie>(replacement).And.NotBeEquivalentTo<ITableData>(expected);
        actual.Version.Should().NotBeEquivalentTo(version);
        actual.UpdatedAt.Should().BeAfter(StartTime);
    }
    #endregion
}
