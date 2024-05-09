// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;
using CommunityToolkit.Datasync.Common.Test;
using System.Net;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using System.Net.Http.Headers;
using CommunityToolkit.Datasync.Client.Test.Helpers;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_AddAsync_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>, IDisposable
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
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("abcdef gh")]
    [InlineData("?")]
    [InlineData(";")]
    [InlineData("{EA235ADF-9F38-44EA-8DA4-EF3D24755767}")]
    [InlineData("###")]
    [InlineData("!!!")]
    public async Task AddAsync_InvalidId_Throws(string id)
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdOnly obj = new() { Id = id };
        Func<Task> act = async () => await sut.AddAsync(obj);

        await act.Should().ThrowAsync<InvalidEntityException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AddAsync_BasicErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdOnly obj = new() { Id = "123" };
        Func<Task> act = async () => await sut.AddAsync(obj);
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == statusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task AddAsync_ConflictErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode) { Content = new StringContent("{\"id\":\"123\"}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdOnly obj = new() { Id = "123" };
        Func<Task> act = async () => await sut.AddAsync(obj);
        ConflictException<T_IdOnly> exception = (await act.Should().ThrowAsync<ConflictException<T_IdOnly>>()).Subject.First();
        exception.StatusCode.Should().Be(statusCode);
        exception.Entity.Id.Should().Be("123");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAsync_ValidContent_Works(bool useNullId)
    {
        T_IdAndTitle expected = new() { Id = useNullId ? null : "123", Title = "Test" };
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"123\",\"title\":\"Test\"}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdAndTitle> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        T_IdAndTitle result = await sut.AddAsync(expected);
        result.Should().NotBeNull().And.BeEquivalentTo(new { Id = "123", Title = "Test" });

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/"));
        request.Should().HaveHeader("X-Test", "value");

        string content = await request.Content.ReadAsStringAsync();
        content.Should().Be(useNullId ? "{\"id\":null,\"title\":\"Test\"}" : "{\"id\":\"123\",\"title\":\"Test\"}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("de76a422-7fb0-4f1f-9bb4-12b3c7882541")]
    public async Task Service_AddAsync_Basic(string id)
    {
        DateTimeOffset startTime = DateTimeOffset.Now;
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        ClientMovie source = new(Movies.BlackPanther) { Id = id };
        ClientMovie result = await dataset.AddAsync(source);
        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(result.Id);

        result.Should().NotBeNull().And.HaveChangedMetadata(id, startTime).And.BeEquivalentTo<IMovie>(source);
        result.Should().HaveEquivalentMetadataTo(serverEntity).And.BeEquivalentTo<IMovie>(serverEntity);
    }

    [Fact]
    public async Task Service_AddAsync_Conflict()
    {
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie source = new(Movies.BlackPanther) { Id = existingMovie.Id };
        Func<Task> act = async () => await dataset.AddAsync(source);

        ConflictException<ClientMovie> exception = (await act.Should().ThrowAsync<ConflictException<ClientMovie>>()).Subject.FirstOrDefault();
        exception.Entity.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }
}
