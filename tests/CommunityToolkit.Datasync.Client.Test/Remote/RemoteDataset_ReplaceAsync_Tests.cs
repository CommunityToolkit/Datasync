// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.Net;
using System.Net.Http.Headers;
using xRetry;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_ReplaceAsync_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>, IDisposable
{
    #region Test setup
    private readonly MockDelegatingHandler mockHandler = new();
    private readonly MediaTypeHeaderValue jsonMediaTypeHeaderValue = new("application/json");

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.mockHandler?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("abcdef gh")]
    [InlineData("?")]
    [InlineData(";")]
    [InlineData("{EA235ADF-9F38-44EA-8DA4-EF3D24755767}")]
    [InlineData("###")]
    [InlineData("!!!")]
    public async Task ReplaceAsync_InvalidId_Throws(string id)
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdOnly obj = new() { Id = id };
        Func<Task> act = async () => await sut.ReplaceAsync(obj);

        await act.Should().ThrowAsync<InvalidEntityException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReplaceAsync_BasicErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdAndVersion> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdAndVersion obj = new() { Id = "123", Version = "v1" };
        Func<Task> act = async () => await sut.ReplaceAsync(obj);
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == statusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task ReplaceAsync_ConflictErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode) { Content = new StringContent("{\"id\":\"123\",\"version\":\"v3\"}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdAndVersion> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdAndVersion obj = new() { Id = "123", Version = "v1" };
        Func<Task> act = async () => await sut.ReplaceAsync(obj);
        ConflictException<T_IdAndVersion> exception = (await act.Should().ThrowAsync<ConflictException<T_IdAndVersion>>()).Subject.First();
        exception.StatusCode.Should().Be(statusCode);
        exception.Entity.Should().BeEquivalentTo(new { Id = "123", Version = "v3" });
    }

    [Fact]
    public async Task ReplaceAsync_ValidContent_Works()
    {
        T_IdAndVersion expected = new() { Id = "123", Version = "v1" };
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"123\",\"version\":\"v2\"}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdAndVersion> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdAndVersion result = await sut.ReplaceAsync(expected);
        result.Should().NotBeNull().And.BeEquivalentTo(new { Id = "123", Version = "v2" });

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/123"));
        request.Should().HaveHeader("X-Test", "value").And.HaveHeader("If-Match", "\"v1\"");

        string content = await request.Content.ReadAsStringAsync();
        content.Should().Be("{\"id\":\"123\",\"version\":\"v1\"}");
    }

    [Fact]
    public async Task ReplaceAsync_ValidContent_Forced_Works()
    {
        T_IdAndVersion expected = new() { Id = "123", Version = "v1" };
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"123\",\"version\":\"v2\"}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdAndVersion> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdAndVersion result = await sut.ReplaceAsync(expected, force: true);
        result.Should().NotBeNull().And.BeEquivalentTo(new { Id = "123", Version = "v2" });

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/123"));
        request.Should().HaveHeader("X-Test", "value").And.NotHaveHeader("If-Match");

        string content = await request.Content.ReadAsStringAsync();
        content.Should().Be("{\"id\":\"123\",\"version\":\"v1\"}");
    }

    [Fact]
    public async Task Service_ReplaceAsync_Forced_Works()
    {
        DateTimeOffset startTime = DateTimeOffset.Now;
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie replacementMovie = new(Movies.BlackPanther) { Id = existingMovie.Id };

        ClientMovie result = await dataset.ReplaceAsync(replacementMovie, force: true);
        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        result.Should().NotBeNull()
            .And.HaveChangedMetadata(existingMovie.Id, startTime)
            .And.HaveEquivalentMetadataTo(serverEntity)
            .And.BeEquivalentTo<IMovie>(replacementMovie)
            .And.BeEquivalentTo<IMovie>(serverEntity);
    }

    [Fact]
    public async Task Service_ReplaceAsync_Versioned_Works()
    {
        DateTimeOffset startTime = DateTimeOffset.Now;
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        InMemoryMovie serverEntity = this.factory.GetRandomMovie();
        ClientMovie movie = await dataset.FindAsync(serverEntity.Id);
        movie.Title = "Black Panther";

        ClientMovie result = await dataset.ReplaceAsync(movie);
        result.Should().NotBeNull().And.HaveChangedMetadata(movie, startTime).And.BeEquivalentTo<IMovie>(movie);

        InMemoryMovie serverResult = this.factory.GetServerEntityById<InMemoryMovie>(movie.Id);
        result.Should().HaveEquivalentMetadataTo(serverResult).And.BeEquivalentTo<IMovie>(serverResult);
    }

    [Fact]
    public async Task Service_ReplaceAsync_Versioned_Conflict()
    {
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        string invalidVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        ClientMovie replacementMovie = new(Movies.BlackPanther) { Id = existingMovie.Id, Version = invalidVersion };
        Func<Task> act = async () => await dataset.ReplaceAsync(replacementMovie);

        ConflictException<ClientMovie> exception = (await act.Should().ThrowAsync<ConflictException<ClientMovie>>()).Subject.FirstOrDefault();
        exception.Entity.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeEquivalentTo<IMovie>(replacementMovie);
    }
}
