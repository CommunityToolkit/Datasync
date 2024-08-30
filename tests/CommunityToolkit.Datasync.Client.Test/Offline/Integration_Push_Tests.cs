// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
[Collection("SynchronizedOfflineTests")]
public class Integration_Push_Tests : ServiceTest, IClassFixture<ServiceApplicationFactory>, IDisposable
{
    private readonly IntegrationDbContext context;

    #region Setup
    public Integration_Push_Tests(ServiceApplicationFactory factory) : base(factory)
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
    public async Task PushAsync_Complex_Situation()
    {
        ResetInMemoryMovies();

        PullResult initialPullResults = await this.context.Movies.PullAsync();
        initialPullResults.IsSuccessful.Should().BeTrue();
        initialPullResults.Additions.Should().Be(248);
        initialPullResults.Deletions.Should().Be(0);
        initialPullResults.Replacements.Should().Be(0);

        // Let's add some new movies
        ClientMovie blackPanther = new(TestCommon.TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(blackPanther);
        await this.context.SaveChangesAsync();

        // And remove any movie that matches some criteria
        List<ClientMovie> moviesToDelete = await this.context.Movies.Where(x => x.Duration > 180).ToListAsync();
        this.context.Movies.RemoveRange(moviesToDelete);
        await this.context.SaveChangesAsync();

        // Then replace all the Unrated movies with a rating of NC17
        List<ClientMovie> moviesToReplace = await this.context.Movies.Where(x => x.Rating == MovieRating.Unrated).ToListAsync();
        moviesToReplace.ForEach(r => 
        { 
            r.Rating = MovieRating.NC17;
            r.Title = r.Title.PadLeft('-');
            this.context.Movies.Update(r); 
        });
        await this.context.SaveChangesAsync();

        // Check the queue.
        List<DatasyncOperation> operations = await this.context.DatasyncOperationsQueue.ToListAsync();
        operations.Count.Should().Be(1 + moviesToDelete.Count + moviesToReplace.Count);
        operations.Count(x => x.Kind is OperationKind.Add).Should().Be(1);
        operations.Count(x => x.Kind is OperationKind.Delete).Should().Be(moviesToDelete.Count);
        operations.Count(x => x.Kind is OperationKind.Replace).Should().Be(moviesToReplace.Count);

        // Now push the results and check what we did
        PushResult pushResults = await this.context.Movies.PushAsync();

        // This little snippet of code is to aid debugging if this test fails
        if (!pushResults.IsSuccessful)
        {
            foreach (KeyValuePair<string, ServiceResponse> failedRequest in pushResults.FailedRequests)
            {
                string id = failedRequest.Key;
                ServiceResponse response = failedRequest.Value;
                string jsonContent = string.Empty;
                if (response.HasContent)
                {
                    using StreamReader reader = new(response.ContentStream);
                    jsonContent = reader.ReadToEnd();
                }

                Console.WriteLine($"FAILED REQUEST FOR ID: {id}: {response.StatusCode}\n{jsonContent}");
            }
        }

        pushResults.IsSuccessful.Should().BeTrue();
        pushResults.CompletedOperations.Should().Be(1 + moviesToDelete.Count + moviesToReplace.Count);
        this.context.DatasyncOperationsQueue.Should().BeEmpty();

        // Now use PullAsync() again - these should all be pulled down again
        PullResult pullResults = await this.context.Movies.PullAsync();
        pullResults.IsSuccessful.Should().BeTrue();
        pullResults.Additions.Should().Be(0);
        pullResults.Deletions.Should().Be(0);
        // The service always replaces additions and replacements - updating the last updatedAt.
        pullResults.Replacements.Should().Be(moviesToReplace.Count + 1); 
    }

    [Fact]
    public async Task PushAsync_ByteVersion()
    {
        ResetInMemoryMovies();

        PullResult initialPullResults = await this.context.ByteMovies.PullAsync();
        initialPullResults.IsSuccessful.Should().BeTrue();
        initialPullResults.Additions.Should().Be(248);
        initialPullResults.Deletions.Should().Be(0);
        initialPullResults.Replacements.Should().Be(0);
        this.context.ByteMovies.Should().HaveCount(248);

        // Let's add some new movies
        ByteVersionMovie blackPanther = new(TestCommon.TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.ByteMovies.Add(blackPanther);
        await this.context.SaveChangesAsync();

        // And remove any movie that matches some criteria
        List<ByteVersionMovie> moviesToDelete = await this.context.ByteMovies.Where(x => x.Duration > 180).ToListAsync();
        this.context.ByteMovies.RemoveRange(moviesToDelete);
        await this.context.SaveChangesAsync();

        // Then replace all the Unrated movies with a rating of NC17
        List<ByteVersionMovie> moviesToReplace = await this.context.ByteMovies.Where(x => x.Rating == MovieRating.Unrated).ToListAsync();
        moviesToReplace.ForEach(r =>
        {
            r.Rating = MovieRating.NC17;
            r.Title = r.Title.PadLeft('-');
            this.context.ByteMovies.Update(r);
        });
        await this.context.SaveChangesAsync();

        // Check the queue.
        List<DatasyncOperation> operations = await this.context.DatasyncOperationsQueue.ToListAsync();
        operations.Count.Should().Be(1 + moviesToDelete.Count + moviesToReplace.Count);
        operations.Count(x => x.Kind is OperationKind.Add).Should().Be(1);
        operations.Count(x => x.Kind is OperationKind.Delete).Should().Be(moviesToDelete.Count);
        operations.Count(x => x.Kind is OperationKind.Replace).Should().Be(moviesToReplace.Count);

        // Now push the results and check what we did
        PushResult pushResults = await this.context.PushAsync();

        // This little snippet of code is to aid debugging if this test fails
        if (!pushResults.IsSuccessful)
        {
            foreach (KeyValuePair<string, ServiceResponse> failedRequest in pushResults.FailedRequests)
            {
                string id = failedRequest.Key;
                ServiceResponse response = failedRequest.Value;
                string jsonContent = string.Empty;
                if (response.HasContent)
                {
                    using StreamReader reader = new(response.ContentStream);
                    jsonContent = reader.ReadToEnd();
                }

                Console.WriteLine($"FAILED REQUEST FOR ID: {id}: {response.StatusCode}\n{jsonContent}");
            }
        }

        pushResults.IsSuccessful.Should().BeTrue();
        pushResults.CompletedOperations.Should().Be(1 + moviesToDelete.Count + moviesToReplace.Count);
        this.context.DatasyncOperationsQueue.Should().BeEmpty();

        // Now use PullAsync() again - these should all be pulled down again
        PullResult pullResults = await this.context.ByteMovies.PullAsync();
        pullResults.IsSuccessful.Should().BeTrue();
        pullResults.Additions.Should().Be(0);
        pullResults.Deletions.Should().Be(0);
        // The service always replaces additions and replacements - updating the last updatedAt.
        pullResults.Replacements.Should().Be(moviesToReplace.Count + 1);
    }

    [Fact]
    public async Task PushAsync_Multithreaded()
    {
        ResetInMemoryMovies();

        PullResult initialPullResults = await this.context.Movies.PullAsync();
        initialPullResults.IsSuccessful.Should().BeTrue();
        initialPullResults.Additions.Should().Be(248);
        initialPullResults.Deletions.Should().Be(0);
        initialPullResults.Replacements.Should().Be(0);

        // Let's add some new movies
        ClientMovie blackPanther = new(TestCommon.TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(blackPanther);
        await this.context.SaveChangesAsync();

        // And remove any movie that matches some criteria
        List<ClientMovie> moviesToDelete = await this.context.Movies.Where(x => x.Duration > 180).ToListAsync();
        this.context.Movies.RemoveRange(moviesToDelete);
        await this.context.SaveChangesAsync();

        // Then replace all the Unrated movies with a rating of NC17
        List<ClientMovie> moviesToReplace = await this.context.Movies.Where(x => x.Rating == MovieRating.Unrated).ToListAsync();
        moviesToReplace.ForEach(r =>
        {
            r.Rating = MovieRating.NC17;
            r.Title = r.Title.PadLeft('-');
            this.context.Movies.Update(r);
        });
        await this.context.SaveChangesAsync();

        // Check the queue.
        List<DatasyncOperation> operations = await this.context.DatasyncOperationsQueue.ToListAsync();
        operations.Count.Should().Be(1 + moviesToDelete.Count + moviesToReplace.Count);
        operations.Count(x => x.Kind is OperationKind.Add).Should().Be(1);
        operations.Count(x => x.Kind is OperationKind.Delete).Should().Be(moviesToDelete.Count);
        operations.Count(x => x.Kind is OperationKind.Replace).Should().Be(moviesToReplace.Count);

        // Now push the results and check what we did
        PushResult pushResults = await this.context.Movies.PushAsync(new PushOptions { ParallelOperations = 8 });

        // This little snippet of code is to aid debugging if this test fails
        if (!pushResults.IsSuccessful)
        {
            foreach (KeyValuePair<string, ServiceResponse> failedRequest in pushResults.FailedRequests)
            {
                string id = failedRequest.Key;
                ServiceResponse response = failedRequest.Value;
                string jsonContent = string.Empty;
                if (response.HasContent)
                {
                    using StreamReader reader = new(response.ContentStream);
                    jsonContent = reader.ReadToEnd();
                }

                Console.WriteLine($"FAILED REQUEST FOR ID: {id}: {response.StatusCode}\n{jsonContent}");
            }
        }

        pushResults.IsSuccessful.Should().BeTrue();
        pushResults.CompletedOperations.Should().Be(1 + moviesToDelete.Count + moviesToReplace.Count);
        this.context.DatasyncOperationsQueue.Should().BeEmpty();

        // Now use PullAsync() again - these should all be pulled down again
        PullResult pullResults = await this.context.Movies.PullAsync();
        pullResults.IsSuccessful.Should().BeTrue();
        pullResults.Additions.Should().Be(0);
        pullResults.Deletions.Should().Be(0);
        // The service always replaces additions and replacements - updating the last updatedAt.
        pullResults.Replacements.Should().Be(moviesToReplace.Count + 1);
    }
}
