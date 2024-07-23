// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Models;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Common;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.OfflineDbContext_Tests;

/// <summary>
/// A set of tests for just the PullAsync() method within the OfflineDbContext
/// </summary>
[ExcludeFromCodeCoverage]
public class PullAsync_Tests
{
    private DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;
    private JsonSerializerOptions SerializerOptions { get; } = new DatasyncServiceOptions().JsonSerializerOptions;

    /// <summary>
    /// Gets a list of the first n movies in the Movie dataset and returns them
    /// as properly configured ClientMovie records.
    /// </summary>
    /// <param name="count">The number of records to return.</param>
    /// <returns>The set of movies.</returns>
    private List<ClientMovie> GetMovies(int count)
    {
        // This is the list of movies - should have everything set once complete, including version and updatedAt
        List<InMemoryMovie> movies = TestData.Movies.OfType<InMemoryMovie>().Take(count).ToList();
        movies.ForEach(m => { m.Version = Guid.NewGuid().ToByteArray(); m.UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5); });
        
        // Serialize, then deserialize to ensure we get the right format.  This is tested elsewhere.
        string serializerContent = JsonSerializer.Serialize(movies, SerializerOptions);
        List<ClientMovie> clientMovies = JsonSerializer.Deserialize<List<ClientMovie>>(serializerContent, SerializerOptions);

        // Return the result.
        return clientMovies;
    }

    [Fact]
    public async Task PullAsync_BasicWorkingDemo_SingleTable()
    {
        // Create the context
        InMemoryOfflineDbContext context = TestDbContextFactory.CreateInMemoryContext();

        // Check to ensure that the SynchronizationSetMetadata is empty
        context.DatasyncMetadata.Should().BeEmpty();

        // Check to ensure that the Movies is empty
        context.Movies.Should().BeEmpty();

        // Place a single page response into the Handler so that we get one page back.
        List<ClientMovie> expectedMovies = GetMovies(10);
        context.HttpHandler.AddResponse(HttpStatusCode.OK, new Page<ClientMovie> { Items = expectedMovies });

        // Do the PullAsync
        PullOperationOptions options = new() { /* TODO: Set up a single query process for the context.Movies */ };
        PullResult result = await context.PullAsync(options);

        // Check that the PullResult is successful.
        result.IsSuccessful.Should().BeTrue();
        result.Operations.Should().HaveCount(1).And.AllSatisfy(x => x.IsSuccessful.Should().BeTrue());

        // Check to ensure that the SynchronizationSetMetadata has one entity in it with the right value.
        SynchronizationSetMetadata metadata = context.DatasyncMetadata.SingleOrDefault(x => x.Id == "/tables/movies");
        metadata.Should().NotBeNull();
        metadata.DeltaToken.Should().BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);

        // Check to ensure that the Movies now has the right information in it.
        List<ClientMovie> actualMovies = await context.Movies.ToListAsync();
        actualMovies.Should().BeEquivalentTo(expectedMovies);

        // HTTP request was for the right URI.
        context.HttpHandler.Requests.Should().HaveCount(1);
        context.HttpHandler.Requests[0].Should().BeHttpGet("http://localhost/tables/movies");
    }
}
