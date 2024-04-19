// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;

namespace CommunityToolkit.Datasync.Server.InMemory.Test;

[ExcludeFromCodeCoverage]
public class InMemoryRepository_Tests : RepositoryTests<InMemoryMovie>
{
    #region Setup
    private InMemoryRepository<InMemoryMovie> repository;

    protected override Task<InMemoryMovie> GetEntityAsync(string id)
        => Task.FromResult(this.repository.GetEntity(id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(this.repository.GetEntities().Count);

    protected override Task<IRepository<InMemoryMovie>> GetPopulatedRepositoryAsync()
    {
        this.repository = new InMemoryRepository<InMemoryMovie>(Movies.OfType<InMemoryMovie>());
        return Task.FromResult<IRepository<InMemoryMovie>>(this.repository);
    }

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
    {
        Random random = new();
        return Task.FromResult(exists ? this.repository.GetEntities()[random.Next(this.repository.GetEntities().Count)].Id : Guid.NewGuid().ToString());
    }
    #endregion

    [Fact]
    public void Ctor_Empty()
    {
        InMemoryRepository<InMemoryMovie> sut = new();

        sut.Should().NotBeNull();
        sut.GetEntities().Should().BeEmpty();
    }

    [Fact]
    public async Task Ctor_Populated()
    {
        InMemoryRepository<InMemoryMovie> sut = await GetPopulatedRepositoryAsync() as InMemoryRepository<InMemoryMovie>;
        int movieCount = Movies.MovieList.Length;

        sut.Should().NotBeNull();
        sut.GetEntities().Count.Should().Be(movieCount);
    }

    [Fact]
    public async Task Remove_NonExisting_Throws()
    {
        InMemoryRepository<InMemoryMovie> sut = await GetPopulatedRepositoryAsync() as InMemoryRepository<InMemoryMovie>;
        Action act = () => sut.RemoveEntity("non-existing-id");
        act.Should().Throw<RepositoryException>();
    }

    [Fact]
    public void Ctor_Populated_NoId()
    {
        List<InMemoryMovie> movies = Movies.OfType<InMemoryMovie>().ConvertAll(m => { m.Id = null; return m; });
        InMemoryRepository<InMemoryMovie> sut = new(movies);

        sut.Should().NotBeNull();
        sut.GetEntities().Count.Should().Be(movies.Count);
    }

    [Fact]
    public async Task AsQueryableAsync_Throws()
    {
        InMemoryRepository<InMemoryMovie> sut = await GetPopulatedRepositoryAsync() as InMemoryRepository<InMemoryMovie>;
        sut.ThrowException = new ApplicationException("test exception");
        Func<Task> act = async () => _ = await sut.AsQueryableAsync();

        await act.Should().ThrowAsync<ApplicationException>();
    }

    [Fact]
    public async Task CreateAsync_Throws_OnForcedException()
    {
        InMemoryRepository<InMemoryMovie> sut = await GetPopulatedRepositoryAsync() as InMemoryRepository<InMemoryMovie>;
        sut.ThrowException = new ApplicationException("test exception");
        InMemoryMovie addition = Movies.OfType<InMemoryMovie>(Movies.BlackPanther);
        Func<Task> act = async () => await sut.CreateAsync(addition);

        await act.Should().ThrowAsync<ApplicationException>();
    }

    [Theory]
    [InlineData("id-002")]
    public async Task DeleteAsync_Throws_OnForcedException(string id)
    {
        InMemoryRepository<InMemoryMovie> sut = await GetPopulatedRepositoryAsync() as InMemoryRepository<InMemoryMovie>;
        sut.ThrowException = new ApplicationException("test exception");
        Func<Task> act = async () => await sut.DeleteAsync(id);

        await act.Should().ThrowAsync<ApplicationException>();
    }

    [Theory]
    [InlineData("id-006")]
    public async Task ReadAsync_Throws_OnForcedException(string id)
    {
        InMemoryRepository<InMemoryMovie> sut = await GetPopulatedRepositoryAsync() as InMemoryRepository<InMemoryMovie>;
        sut.ThrowException = new ApplicationException("test exception");
        Func<Task> act = async () => _ = await sut.ReadAsync(id);

        await act.Should().ThrowAsync<ApplicationException>();
    }

    [Theory]
    [InlineData("id-008")]
    public async Task ReplaceAsync_Throws_OnForcedException(string id)
    {
        InMemoryRepository<InMemoryMovie> sut = await GetPopulatedRepositoryAsync() as InMemoryRepository<InMemoryMovie>;
        sut.ThrowException = new ApplicationException("test exception");
        InMemoryMovie replacement = Movies.OfType<InMemoryMovie>(Movies.BlackPanther, id);
        Func<Task> act = async () => await sut.ReplaceAsync(replacement);

        await act.Should().ThrowAsync<ApplicationException>();
    }
}
