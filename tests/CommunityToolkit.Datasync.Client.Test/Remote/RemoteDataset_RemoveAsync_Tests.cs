// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.Net;
using System.Net.Http.Headers;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_RemoveAsync_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>, IDisposable
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

    #region RemoveAsync
    [Fact]
    public async Task RemoveAsync_NullId_Throws()
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.RemoveAsync(null);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("abcdef gh")]
    [InlineData("?")]
    [InlineData(";")]
    [InlineData("{EA235ADF-9F38-44EA-8DA4-EF3D24755767}")]
    [InlineData("###")]
    [InlineData("!!!")]
    public async Task RemoveAsync_InvalidId_Throws(string id)
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.RemoveAsync(id);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task RemoveAsync_BasicErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.RemoveAsync("123");
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == statusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task RemoveAsync_ConflictErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode) { Content = new StringContent("{\"id\":\"123\"}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        Func<Task> act = async () => await sut.RemoveAsync("123");
        ConflictException<T_IdOnly> exception = (await act.Should().ThrowAsync<ConflictException<T_IdOnly>>()).Subject.First();
        exception.StatusCode.Should().Be(statusCode);
        exception.Entity.Id.Should().Be("123");
    }

    [Fact]
    public async Task RemoveAsync_ValidContent_Works()
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.NoContent));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        await sut.RemoveAsync("123");

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/123"));
        request.Should().HaveHeader("X-Test", "value");
    }

    [Fact]
    public async Task RemoveAsync_ConditionalRequest_Works()
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.NoContent));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        await sut.RemoveAsync("123", "v1");

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/123"));
        request.Should().HaveHeader("X-Test", "value").And.HaveHeader("If-Match", "\"v1\"");
    }

    [Fact]
    public async Task Service_RemoveAsync_Forced_Works()
    {
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        await dataset.RemoveAsync(existingMovie.Id);

        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().BeNull();
    }

    [Fact]
    public async Task Service_RemoveAsync_Versioned_Works()
    {
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        await dataset.RemoveAsync(existingMovie.Id, Convert.ToBase64String(existingMovie.Version));

        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().BeNull();
    }

    [Fact]
    public async Task Service_RemoveAsync_Versioned_Conflict()
    {
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        Func<Task> act = async () => await dataset.RemoveAsync(existingMovie.Id, Guid.NewGuid().ToString("N"));

        ConflictException<ClientMovie> exception = (await act.Should().ThrowAsync<ConflictException<ClientMovie>>()).Subject.FirstOrDefault();
        exception.Entity.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeNull();
    }
    #endregion
}
