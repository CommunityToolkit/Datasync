// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define CAPTURE_STRING_JSON

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Server.Test.Service;

[ExcludeFromCodeCoverage]
public class Replace_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Fact]
    public async Task Replace_Returns200()
    {
        ClientMovie existingMovie = new(this.factory.GetRandomMovie()) { Title = "New Title" };
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.MovieEndpoint}/{existingMovie.Id}", existingMovie, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

#if CAPTURE_STRING_JSON
        string jsonContent = await response.Content.ReadAsStringAsync();
        ClientMovie clientMovie = JsonSerializer.Deserialize<ClientMovie>(jsonContent, this.serializerOptions);
#else
        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
#endif
        
        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(existingMovie, this.StartTime).And.BeEquivalentTo<IMovie>(existingMovie);
        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }

    [Theory]
    [InlineData("If-Match", null, HttpStatusCode.OK)]
    [InlineData("If-Match", "\"dGVzdA==\"", HttpStatusCode.PreconditionFailed)]
    [InlineData("If-None-Match", null, HttpStatusCode.PreconditionFailed)]
    [InlineData("If-None-Match", "\"dGVzdA==\"", HttpStatusCode.OK)]
    public async Task Replace_WithVersioning_Works(string headerName, string headerValue, HttpStatusCode expectedStatusCode)
    {
        ClientMovie existingMovie = new(this.factory.GetRandomMovie());
        string etag = headerValue ?? $"\"{existingMovie.Version}\"";
        string content = JsonSerializer.Serialize(existingMovie);

        HttpRequestMessage request = new(HttpMethod.Put, $"{this.factory.MovieEndpoint}/{existingMovie.Id}") { Content = new StringContent(content, Encoding.UTF8, "application/json") };
        request.Headers.Add(headerName, etag);

        HttpResponseMessage response = await this.client.SendAsync(request);
        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode == HttpStatusCode.OK)
        {
            ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
            clientMovie.Should().NotBeNull().And.HaveChangedMetadata(existingMovie, this.StartTime).And.BeEquivalentTo<IMovie>(existingMovie);

            InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
            clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
            response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
        }
    }

    [Fact]
    public async Task Replace_MissingId_Returns404()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie source = new(existingMovie) { Id = "missing" };
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.MovieEndpoint}/missing", source, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Movie = {0}", existingMovie.Id);
    }

    [Fact]
    public async Task Replace_IdMismatch_Returns400()
    {
        ClientMovie existingMovie = new(this.factory.GetRandomMovie()) { Title = "New Title" };
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.MovieEndpoint}/different-id", existingMovie, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Replace_NotSoftDeleted_Works()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}", existingMovie, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(existingMovie, this.StartTime).And.BeEquivalentTo<IMovie>(existingMovie);

        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }

    [Fact]
    public async Task Replace_SoftDeleted_NotUndeleting_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}", existingMovie, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Replace_SoftDeleted_Undeleting_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}", existingMovie, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Replace_SoftDeleted_Undeleting_WithShowDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        existingMovie.Deleted = false;
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}?__includedeleted=true", existingMovie, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(existingMovie, this.StartTime).And.BeEquivalentTo<IMovie>(existingMovie);
        clientMovie.Deleted.Should().BeFalse();

        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }

    [Fact]
    public async Task Replace_NonJsonData_Returns415()
    {
        const string content = "<html><body><h1>Not JSON</h1></body></html>";
        HttpResponseMessage response = await this.client.PutAsync($"{this.factory.MovieEndpoint}/1", new StringContent(content, Encoding.UTF8, "text/html"));
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    /// <summary>
    /// Given an existing entity and an access provider, ensure that we can replace the entity
    /// and that the access provider is called with the existing entity, not the new entity.
    /// </summary>
    [Fact]
    public async Task Replace_Unauthorized_Returns401()
    {
        // Set up the movie access control provider so that is returns false for the update operation
        this.factory.SetupAccessControlProvider(false);

        InMemoryMovie inMemoryMovie = this.factory.GetRandomMovie();
        ClientMovie existingMovie = new(inMemoryMovie) { Title = "New Title" };
        HttpResponseMessage response = await this.client.PutAsJsonAsync($"{this.factory.AuthorizedMovieEndpoint}/{existingMovie.Id}", existingMovie, this.serializerOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Ensure that the access provider was called with the existing movie, not the new movie
        InMemoryMovie lastEntity = this.factory.GetAuthorizedEntity();
        lastEntity.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
    }
}
