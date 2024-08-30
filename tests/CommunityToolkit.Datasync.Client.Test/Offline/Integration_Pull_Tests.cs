// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
[Collection("SynchronizedOfflineTests")]
public class Integration_Pull_Tests : ServiceTest, IClassFixture<ServiceApplicationFactory>, IDisposable
{
    private readonly IntegrationDbContext context;

    #region Setup
    public Integration_Pull_Tests(ServiceApplicationFactory factory) : base(factory)
    {
        this.context = GetOfflineContext();
    }
    #endregion

    #region Tear Down
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.context.Dispose();
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    [Fact]
    public async Task PullAsync_ViaDbSet_Works()
    {
        await this.context.Movies.PullAsync();
        List<ClientMovie> movies = await this.context.Movies.ToListAsync();

        movies.Count.Should().Be(248);
        foreach (ClientMovie movie in movies)
        {
            InMemoryMovie serviceMovie = GetServerEntityById<InMemoryMovie>(movie.Id);
            serviceMovie.Should().NotBeNull()
                .And.BeEquivalentTo<IMovie>(serviceMovie)
                .And.HaveEquivalentMetadataTo(serviceMovie);
        }
    }

    [Fact]
    public async Task PullAsync_ViaDbSet_Works_ByteVersion()
    {
        await this.context.ByteMovies.PullAsync();
        List<ByteVersionMovie> movies = await this.context.ByteMovies.ToListAsync();

        movies.Count.Should().Be(248);
        foreach (ByteVersionMovie movie in movies)
        {
            InMemoryMovie serviceMovie = GetServerEntityById<InMemoryMovie>(movie.Id);
            serviceMovie.Should().NotBeNull()
                .And.BeEquivalentTo<IMovie>(serviceMovie)
                .And.HaveEquivalentMetadataTo(serviceMovie);
        }
    }

    [Fact]
    public async Task PullAsync_ViaContext_Works()
    {
        await this.context.PullAsync();
        List<ClientMovie> movies = await this.context.Movies.ToListAsync();

        movies.Count.Should().Be(248);
        foreach (ClientMovie movie in movies)
        {
            InMemoryMovie serviceMovie = GetServerEntityById<InMemoryMovie>(movie.Id);
            serviceMovie.Should().NotBeNull()
                .And.BeEquivalentTo<IMovie>(serviceMovie)
                .And.HaveEquivalentMetadataTo(serviceMovie);
        }
    }

    [Fact]
    public async Task PullAsync_ViaConfigurator_Works()
    {
        await this.context.PullAsync(builder =>
        {
            builder.SetParallelOperations(2);

            builder.AddPullRequest<ClientMovie>(opt =>
            {
                opt.QueryId = "pg-rated";
                opt.Query.Where(x => x.Rating == MovieRating.PG);
            });

            builder.AddPullRequest<ClientMovie>(opt =>
            {
                opt.QueryId = "pg13-rated";
                opt.Query.Where(x => x.Rating == MovieRating.PG13);
            });
        });
        List<ClientMovie> movies = await this.context.Movies.ToListAsync();

        int expectedCount = TestCommon.TestData.Movies.MovieList.Count(x => x.Rating is MovieRating.PG or MovieRating.PG13);
        movies.Count.Should().Be(expectedCount);
        foreach (ClientMovie movie in movies)
        {
            InMemoryMovie serviceMovie = GetServerEntityById<InMemoryMovie>(movie.Id);
            serviceMovie.Should().NotBeNull()
                .And.BeEquivalentTo<IMovie>(serviceMovie)
                .And.HaveEquivalentMetadataTo(serviceMovie);
        }
    }
}
