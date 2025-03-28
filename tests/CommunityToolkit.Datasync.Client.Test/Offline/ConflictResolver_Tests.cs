// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

#pragma warning disable IDE0008 // Use explicit type

[ExcludeFromCodeCoverage]
[Collection("SynchronizedOfflineTests")]
public class ConflictResolver_Tests : BaseTest
{
    #region Built-in Conflict Resolvers Tests

    [Fact]
    public async Task ClientWinsConflictResolver_ShouldReturnClientObject()
    {
        // Arrange
        var clientResolver = new ClientWinsConflictResolver();
        var clientObject = new ClientMovie(TestData.Movies.BlackPanther) { Id = "test-id" };
        var serverObject = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = "test-id",
            Title = "Updated on Server"
        };

        // Act
        var result = await clientResolver.ResolveConflictAsync(clientObject, serverObject);

        // Assert
        result.Should().BeSameAs(clientObject);
    }

    [Fact]
    public async Task ServerWinsConflictResolver_ShouldReturnServerObject()
    {
        // Arrange
        var serverResolver = new ServerWinsConflictResolver();
        var clientObject = new ClientMovie(TestData.Movies.BlackPanther) { Id = "test-id" };
        var serverObject = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = "test-id",
            Title = "Updated on Server"
        };

        // Act
        var result = await serverResolver.ResolveConflictAsync(clientObject, serverObject);

        // Assert
        result.Should().BeSameAs(serverObject);
    }

    [Fact]
    public async Task ClientWinsConflictResolver_ShouldReturnClientObject_WithNullServerObject()
    {
        // Arrange
        var clientResolver = new ClientWinsConflictResolver();
        var clientObject = new ClientMovie(TestData.Movies.BlackPanther) { Id = "test-id" };

        // Act
        var result = await clientResolver.ResolveConflictAsync(clientObject, null);

        // Assert
        result.Should().BeSameAs(clientObject);
    }

    [Fact]
    public async Task ServerWinsConflictResolver_ShouldReturnServerObject_WithNullClientObject()
    {
        // Arrange
        var serverResolver = new ServerWinsConflictResolver();
        var serverObject = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = "test-id",
            Title = "Updated on Server"
        };

        // Act
        var result = await serverResolver.ResolveConflictAsync(null, serverObject);

        // Assert
        result.Should().BeSameAs(serverObject);
    }

    #endregion

    #region Generic Conflict Resolver Tests

    [Fact]
    public async Task GenericConflictResolver_ShouldResolveTypedConflict()
    {
        // Arrange
        var resolver = new TestGenericConflictResolver();
        var clientObject = new ClientMovie(TestData.Movies.BlackPanther) { Id = "test-id", Rating = MovieRating.R };
        var serverObject = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = "test-id",
            Title = "Updated on Server",
            Rating = MovieRating.G
        };

        // Act
        var result = await resolver.ResolveConflictAsync(clientObject, serverObject);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(serverObject.Title); // From server
        result.Rating.Should().Be(clientObject.Rating); // From client
    }

    [Fact]
    public async Task GenericConflictResolver_ObjectMethod_ShouldCallTypedMethod()
    {
        // Arrange
        var resolver = new TestGenericConflictResolver();
        var clientObject = new ClientMovie(TestData.Movies.BlackPanther) { Id = "test-id", Rating = MovieRating.R };
        var serverObject = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = "test-id",
            Title = "Updated on Server",
            Rating = MovieRating.G
        };

        // Act
        var result = await resolver.ResolveConflictAsync((object)clientObject, (object)serverObject);

        // Assert
        result.Should().NotBeNull();
        var movie = result as ClientMovie;
        movie.Should().NotBeNull();
        movie!.Title.Should().Be(serverObject.Title); // From server
        movie.Rating.Should().Be(clientObject.Rating); // From client
    }

    #endregion

    #region Integration with OperationsQueueManager Tests

    [Fact]
    public async Task PushAsync_WithClientWinsResolver_ShouldResolveConflictAndRetry()
    {
        // Arrange
        var context = CreateContext();

        // Configure context to use client wins resolver
        context.Configurator = builder =>
        {
            builder.Entity<ClientMovie>(c =>
            {
                c.ClientName = "movies";
                c.Endpoint = new Uri("/tables/movies", UriKind.Relative);
                c.ConflictResolver = new ClientWinsConflictResolver();
            });
        };

        // Create a client movie and save it to generate operation
        var clientMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Client Title"
        };
        context.Movies.Add(clientMovie);
        context.SaveChanges();

        // Setup response for conflict followed by success
        var serverMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = Guid.NewGuid().ToString()
        };
        string serverJson = DatasyncSerializer.Serialize(serverMovie);

        // First response is a conflict
        context.Handler.AddResponseContent(serverJson, HttpStatusCode.Conflict);

        // Second response (after resolution) is success
        var finalMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Client Title",  // This should match the client version after resolution
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(1),
            Version = Guid.NewGuid().ToString()
        };
        string finalJson = DatasyncSerializer.Serialize(finalMovie);
        context.Handler.AddResponseContent(finalJson, HttpStatusCode.OK);

        // Act
        var result = await context.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CompletedOperations.Should().Be(1);
        result.FailedRequests.Should().BeEmpty();

        // Verify the database has the right value
        var savedMovie = context.Movies.Find(clientMovie.Id);
        savedMovie.Should().NotBeNull();
        savedMovie!.Title.Should().Be("Client Title");
        savedMovie.Version.Should().Be(finalMovie.Version);
    }

    [Fact]
    public async Task PushAsync_WithServerWinsResolver_ShouldResolveConflictAndRetry()
    {
        // Arrange
        var context = CreateContext();

        // Configure context to use server wins resolver
        context.Configurator = builder => builder.Entity<ClientMovie>(c =>
            c.ConflictResolver = new ServerWinsConflictResolver());

        // Create a client movie and save it to generate operation
        var clientMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Client Title"
        };
        context.Movies.Add(clientMovie);
        context.SaveChanges();

        // Setup response for conflict followed by success
        var serverMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = Guid.NewGuid().ToString()
        };
        string serverJson = DatasyncSerializer.Serialize(serverMovie);

        // First response is a conflict
        context.Handler.AddResponseContent(serverJson, HttpStatusCode.Conflict);

        // Second response (after resolution) is success
        var finalMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",  // This should match the server version after resolution
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(1),
            Version = Guid.NewGuid().ToString()
        };
        string finalJson = DatasyncSerializer.Serialize(finalMovie);
        context.Handler.AddResponseContent(finalJson, HttpStatusCode.OK);

        // Act
        var result = await context.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CompletedOperations.Should().Be(1);
        result.FailedRequests.Should().BeEmpty();

        // Verify the database has the right value
        var savedMovie = context.Movies.Find(clientMovie.Id);
        savedMovie.Should().NotBeNull();
        savedMovie!.Title.Should().Be("Server Title");
        savedMovie.Version.Should().Be(finalMovie.Version);
    }

    [Fact]
    public async Task PushAsync_WithCustomResolver_ShouldResolveConflictAndRetry()
    {
        // Arrange
        var context = CreateContext();

        // Configure context to use custom resolver
        context.Configurator = builder => builder.Entity<ClientMovie>(c =>
            c.ConflictResolver = new TestGenericConflictResolver());

        // Create a client movie and save it to generate operation
        var clientMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Client Title",
            Rating = MovieRating.G
        };
        context.Movies.Add(clientMovie);
        context.SaveChanges();

        // Setup response for conflict followed by success
        var serverMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",
            Rating = MovieRating.R,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = Guid.NewGuid().ToString()
        };
        string serverJson = DatasyncSerializer.Serialize(serverMovie);

        // First response is a conflict
        context.Handler.AddResponseContent(serverJson, HttpStatusCode.Conflict);

        // Second response (after resolution) is success
        var finalMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",  // From server
            Rating = MovieRating.G,  // From client
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(1),
            Version = Guid.NewGuid().ToString()
        };
        string finalJson = DatasyncSerializer.Serialize(finalMovie);
        context.Handler.AddResponseContent(finalJson, HttpStatusCode.OK);

        // Act
        var result = await context.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CompletedOperations.Should().Be(1);
        result.FailedRequests.Should().BeEmpty();

        // Verify the database has the right value
        var savedMovie = context.Movies.Find(clientMovie.Id);
        savedMovie.Should().NotBeNull();
        savedMovie!.Title.Should().Be("Server Title"); // From server
        savedMovie.Rating.Should().Be(MovieRating.G); // From client
        savedMovie.Version.Should().Be(finalMovie.Version);
    }

    [Fact]
    public async Task PushAsync_WithPreconditionFailed_ShouldResolveConflict()
    {
        // Arrange
        var context = CreateContext();

        // Configure context to use server wins resolver
        context.Configurator = builder => builder.Entity<ClientMovie>(c =>
            c.ConflictResolver = new ServerWinsConflictResolver());

        // Create a client movie and save it to generate operation
        var clientMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Client Title"
        };
        context.Movies.Add(clientMovie);
        context.SaveChanges();

        // Setup response for conflict followed by success
        var serverMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = Guid.NewGuid().ToString()
        };
        string serverJson = DatasyncSerializer.Serialize(serverMovie);

        // First response is a precondition failed (412)
        context.Handler.AddResponseContent(serverJson, HttpStatusCode.PreconditionFailed);

        // Second response (after resolution) is success
        var finalMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",  // This should match the server version after resolution
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(1),
            Version = Guid.NewGuid().ToString()
        };
        string finalJson = DatasyncSerializer.Serialize(finalMovie);
        context.Handler.AddResponseContent(finalJson, HttpStatusCode.OK);

        // Act
        var result = await context.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CompletedOperations.Should().Be(1);
        result.FailedRequests.Should().BeEmpty();

        // Verify the database has the right value
        var savedMovie = context.Movies.Find(clientMovie.Id);
        savedMovie.Should().NotBeNull();
        savedMovie!.Title.Should().Be("Server Title");
        savedMovie.Version.Should().Be(finalMovie.Version);
    }

    [Fact]
    public async Task PushAsync_WithDeleteOperation_AndConflict_ShouldResolveConflict()
    {
        // Arrange
        var context = CreateContext();

        // Configure context to use client wins resolver
        context.Configurator = builder => builder.Entity<ClientMovie>(c =>
            c.ConflictResolver = new ClientWinsConflictResolver());

        // Create a client movie first
        var clientMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Original Title"
        };
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        // Now delete it to create a delete operation
        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        // Setup response for conflict followed by success
        var serverMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Updated on server",
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = Guid.NewGuid().ToString()
        };
        string serverJson = DatasyncSerializer.Serialize(serverMovie);

        // First response is a conflict
        context.Handler.AddResponseContent(serverJson, HttpStatusCode.Conflict);

        // Second response (after resolution) is success - a deleted entity should return 204 No Content
        context.Handler.AddResponse(HttpStatusCode.NoContent);

        // Act
        var result = await context.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.CompletedOperations.Should().Be(1);
        result.FailedRequests.Should().BeEmpty();

        // Verify the entity has been deleted
        var savedMovie = context.Movies.Find(clientMovie.Id);
        savedMovie.Should().BeNull();
    }

    [Fact]
    public async Task PushAsync_WithNull_ConflictResolver_ShouldNotResolveConflict()
    {
        // Arrange
        var context = CreateContext();

        // Create a client movie and save it to generate operation
        var clientMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Client Title"
        };
        context.Movies.Add(clientMovie);
        context.SaveChanges();

        // Setup conflict response
        var serverMovie = new ClientMovie(TestData.Movies.BlackPanther)
        {
            Id = clientMovie.Id,
            Title = "Server Title",
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = Guid.NewGuid().ToString()
        };
        string serverJson = DatasyncSerializer.Serialize(serverMovie);
        context.Handler.AddResponseContent(serverJson, HttpStatusCode.Conflict);

        // Act
        var result = await context.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.CompletedOperations.Should().Be(0);
        result.FailedRequests.Should().HaveCount(1);

        // Verify operation is still in queue and marked as failed
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        var op = context.DatasyncOperationsQueue.Single();
        op.State.Should().Be(OperationState.Failed);
        op.HttpStatusCode.Should().Be(409);
    }

    #endregion
}

/// <summary>
/// A test conflict resolver implementation that merges client and server objects,
/// taking the Title from the server and the Rating from the client.
/// </summary>
public class TestGenericConflictResolver : AbstractConflictResolver<ClientMovie>
{
    public override Task<ClientMovie> ResolveConflictAsync(ClientMovie clientObject, ClientMovie serverObject, CancellationToken cancellationToken = default)
    {
        if (clientObject == null)
        {
            return Task.FromResult(serverObject);
        }

        if (serverObject == null)
        {
            return Task.FromResult(clientObject);
        }

        // Create a merged object - take title from server but rating from client
        ClientMovie mergedMovie = new(serverObject)
        {
            // Keep these from server:
            Id = serverObject.Id,
            Title = serverObject.Title,
            UpdatedAt = serverObject.UpdatedAt,
            Version = serverObject.Version,

            // Take these from client:
            Rating = clientObject.Rating
        };

        return Task.FromResult<ClientMovie>(mergedMovie);
    }
}
