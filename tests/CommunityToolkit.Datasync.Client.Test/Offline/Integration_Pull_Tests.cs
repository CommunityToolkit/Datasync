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

    [Fact]
    public async Task PullAsync_WithLocalData_Works()
    {
        const string testId = "id-010";

        await this.context.MoviesWithLocalData.PullAsync();

        ClientMovieWithLocalData t1 = await this.context.MoviesWithLocalData.FindAsync([testId]);

        // Update the local data part and push it back to the server.
        t1.UserRating = 5;
        this.context.Update(t1);
        await this.context.SaveChangesAsync();
        await this.context.MoviesWithLocalData.PushAsync();

        // Reload the local data from the server and check that the local data is still there
        await this.context.Entry(t1).ReloadAsync();
        t1.UserRating.Should().Be(5);

        // Pull again and check that the local data is still there.
        await this.context.MoviesWithLocalData.PullAsync();
        ClientMovieWithLocalData t2 = await this.context.MoviesWithLocalData.FindAsync([testId]);
        t2.UserRating.Should().Be(5);

        // Do another change (this time, server side) and push again
        t2.Title = "New Title";
        this.context.Update(t2);
        await this.context.SaveChangesAsync();
        await this.context.MoviesWithLocalData.PushAsync();

        // Reload the local data from the server and check that the local data is still there
        await this.context.Entry(t1).ReloadAsync();
        t2.UserRating.Should().Be(5);
        t2.Title.Should().Be("New Title");

        // Pull again and check that the local data is still there.
        await this.context.MoviesWithLocalData.PullAsync();
        ClientMovieWithLocalData t3 = await this.context.MoviesWithLocalData.FindAsync([testId]);
        t3.UserRating.Should().Be(5);
        t3.Title.Should().Be("New Title");
    }
}
