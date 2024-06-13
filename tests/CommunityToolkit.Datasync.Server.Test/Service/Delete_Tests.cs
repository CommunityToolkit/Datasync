// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net;
using System.Net.Http.Json;

namespace CommunityToolkit.Datasync.Server.Test.Service;

[ExcludeFromCodeCoverage]
public class Delete_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Fact]
    public async Task Delete_ById_Returns204()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();

        HttpResponseMessage response = await this.client.DeleteAsync($"{this.factory.MovieEndpoint}/{existingMovie.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.NoContent);

        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().BeNull();
    }

    [Theory]
    [InlineData("If-Match", null, HttpStatusCode.NoContent)]
    [InlineData("If-Match", "\"dGVzdA==\"", HttpStatusCode.PreconditionFailed)]
    [InlineData("If-None-Match", null, HttpStatusCode.PreconditionFailed)]
    [InlineData("If-None-Match", "\"dGVzdA==\"", HttpStatusCode.NoContent)]
    public async Task Delete_WithVersioning_Works(string headerName, string value, HttpStatusCode expectedStatusCode)
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        string etag = value ?? $"\"{Convert.ToBase64String(existingMovie.Version)}\"";

        HttpRequestMessage request = new(HttpMethod.Delete, $"{this.factory.MovieEndpoint}/{existingMovie.Id}");
        request.Headers.Add(headerName, etag);

        HttpResponseMessage response = await this.client.SendAsync(request);
        response.Should().HaveStatusCode(expectedStatusCode);

        if (expectedStatusCode == HttpStatusCode.PreconditionFailed)
        {
            InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
            ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
            clientMovie.Should().HaveEquivalentMetadataTo(serverEntity).And.BeEquivalentTo<IMovie>(serverEntity);
        }
    }

    [Fact]
    public async Task Delete_MissingId_Returns404()
    {
        HttpResponseMessage response = await this.client.DeleteAsync($"{this.factory.MovieEndpoint}/missing");
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NotSoftDeleted_Returns204()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        byte[] existingVersion = [.. existingMovie.Version];
        DateTimeOffset existingUpdatedAt = (DateTimeOffset)existingMovie.UpdatedAt;

        HttpResponseMessage response = await this.client.DeleteAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.NoContent);

        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeNull();
        serverEntity.UpdatedAt.Should().NotBe(existingUpdatedAt).And.BeAfter(this.StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        serverEntity.Version.Should().NotBeEquivalentTo(existingVersion);
        serverEntity.Deleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_SoftDeletedId_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);

        HttpResponseMessage response = await this.client.DeleteAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.Gone);

        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeNull();
    }
}
