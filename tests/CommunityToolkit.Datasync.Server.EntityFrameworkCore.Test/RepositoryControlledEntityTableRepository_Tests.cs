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

[ExcludeFromCodeCoverage]
public class RepositoryControlledEntityTableRepository_Tests : RepositoryTests<RepositoryControlledEntityMovie>
{
    #region Setup
    private readonly Random random = new();
    private readonly List<RepositoryControlledEntityMovie> movies;
    private readonly Lazy<RepositoryControlledDbContext> lazyContext;

    public RepositoryControlledEntityTableRepository_Tests(ITestOutputHelper output)
    {
        this.lazyContext = new(() => RepositoryControlledDbContext.CreateContext(output));
        this.movies = [.. Context.Movies.AsNoTracking()];
    }

    private RepositoryControlledDbContext Context { get => this.lazyContext.Value; }

    protected override Task<RepositoryControlledEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<RepositoryControlledEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<RepositoryControlledEntityMovie>>(new EntityTableRepository<RepositoryControlledEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(Context.Movies.Count())].Id : Guid.NewGuid().ToString());
    #endregion

    [SkippableFact]
    public async Task IdGenerator_Ulid_CanCreate()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<RepositoryControlledEntityMovie> repository = await GetPopulatedRepositoryAsync();
        string generatedId = string.Empty;
        ((EntityTableRepository<RepositoryControlledEntityMovie>)repository).IdGenerator = _ => { generatedId = Ulid.NewUlid().ToString(); return generatedId; };

        RepositoryControlledEntityMovie addition = TestData.Movies.OfType<RepositoryControlledEntityMovie>(TestData.Movies.BlackPanther);
        addition.Id = null;
        RepositoryControlledEntityMovie sut = addition.Clone();
        await repository.CreateAsync(sut);
        RepositoryControlledEntityMovie actual = await GetEntityAsync(sut.Id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.UpdatedAt.Should().BeAfter(StartTime);
        generatedId.Should().NotBeNullOrEmpty();
        actual.Id.Should().Be(generatedId);
    }

    [SkippableFact]
    public async Task VersionGenerator_Ticks_CanCreate()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<RepositoryControlledEntityMovie> repository = await GetPopulatedRepositoryAsync();
        byte[] generatedVersion = [];
        ((EntityTableRepository<RepositoryControlledEntityMovie>)repository).VersionGenerator = () =>
        {
            DateTimeOffset offset = DateTimeOffset.UtcNow;
            generatedVersion = BitConverter.GetBytes(offset.Ticks);
            return generatedVersion;
        };

        RepositoryControlledEntityMovie addition = TestData.Movies.OfType<RepositoryControlledEntityMovie>(TestData.Movies.BlackPanther);
        addition.Id = null;
        RepositoryControlledEntityMovie sut = addition.Clone();
        await repository.CreateAsync(sut);
        RepositoryControlledEntityMovie actual = await GetEntityAsync(sut.Id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.UpdatedAt.Should().BeAfter(StartTime);
        generatedVersion.Should().NotBeNullOrEmpty();
        actual.Version.Should().BeEquivalentTo(generatedVersion);
    }
}
