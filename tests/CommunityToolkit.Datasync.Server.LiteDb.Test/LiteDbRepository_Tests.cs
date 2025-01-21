// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using LiteDB;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Server.LiteDb.Test;

[ExcludeFromCodeCoverage]
public class LiteDbRepository_Tests : RepositoryTests<LiteDbMovie>, IDisposable
{
    #region Setup
    private string dbFilename;
    private LiteDatabase database;
    private ILiteCollection<LiteDbMovie> collection;
    private LiteDbRepository<LiteDbMovie> repository;
    private readonly List<LiteDbMovie> movies = [];

    protected override Task<LiteDbMovie> GetEntityAsync(string id)
        => Task.FromResult(this.collection.FindById(id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(this.collection.Count());

    protected override Task<IRepository<LiteDbMovie>> GetPopulatedRepositoryAsync()
    {
        this.dbFilename = Path.GetTempFileName();
        this.database = new LiteDatabase($"Filename={this.dbFilename};Connection=direct;InitialSize=0");
        this.collection = this.database.GetCollection<LiteDbMovie>("litedbmovies");

        foreach (LiteDbMovie movie in TestData.Movies.OfType<LiteDbMovie>())
        {
            movie.UpdatedAt = DateTimeOffset.Now;
            movie.Version = Guid.NewGuid().ToByteArray();
            this.collection.Insert(movie);
            this.movies.Add(movie);
        }

        this.repository = new LiteDbRepository<LiteDbMovie>(this.database);
        return Task.FromResult<IRepository<LiteDbMovie>>(this.repository);
    }

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
    {
        Random random = new();
        return Task.FromResult(exists ? this.movies[random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.database?.Dispose();
            if (!string.IsNullOrEmpty(this.dbFilename))
            {
                File.Delete(this.dbFilename);
            }
        }
    }
    #endregion

    [SkippableFact]
    public async Task IdGenerator_Ulid_CanCreate()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<LiteDbMovie> repository = await GetPopulatedRepositoryAsync();
        string generatedId = string.Empty;
        ((LiteDbRepository<LiteDbMovie>)repository).IdGenerator = _ => { generatedId = Ulid.NewUlid().ToString(); return generatedId; };

        LiteDbMovie addition = TestData.Movies.OfType<LiteDbMovie>(TestData.Movies.BlackPanther);
        addition.Id = null;
        LiteDbMovie sut = addition.Clone();
        await repository.CreateAsync(sut);
        LiteDbMovie actual = await GetEntityAsync(sut.Id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.UpdatedAt.Should().BeAfter(StartTime);
        generatedId.Should().NotBeNullOrEmpty();
        actual.Id.Should().Be(generatedId);
    }

    [SkippableFact]
    public async Task VersionGenerator_Ticks_CanCreate()
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<LiteDbMovie> repository = await GetPopulatedRepositoryAsync();
        byte[] generatedVersion = [];
        ((LiteDbRepository<LiteDbMovie>)repository).VersionGenerator = () =>
        {
            DateTimeOffset offset = DateTimeOffset.UtcNow;
            generatedVersion = BitConverter.GetBytes(offset.Ticks);
            return generatedVersion;
        };

        LiteDbMovie addition = TestData.Movies.OfType<LiteDbMovie>(TestData.Movies.BlackPanther);
        addition.Id = null;
        LiteDbMovie sut = addition.Clone();
        await repository.CreateAsync(sut);
        LiteDbMovie actual = await GetEntityAsync(sut.Id);

        actual.Should().BeEquivalentTo<IMovie>(addition);
        actual.UpdatedAt.Should().BeAfter(StartTime);
        generatedVersion.Should().NotBeNullOrEmpty();
        actual.Version.Should().BeEquivalentTo(generatedVersion);
    }
}
