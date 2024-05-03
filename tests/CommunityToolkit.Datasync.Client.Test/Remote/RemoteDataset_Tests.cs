// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.Net;
using System.Net.Http.Headers;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>, IDisposable
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

    #region Constructors and helper methods
    [Fact]
    public void Ctor_Uri_Null_Throws()
    {
        Action act = () => _ = new RemoteDataset<ClientMovie>(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("file://localhost/foo", false)]
    [InlineData("http://foo.azurewebsites.net", false)]
    [InlineData("http://foo.azure-api.net", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000/myapi", false)]
    [InlineData("http://10.0.0.8", false)]
    [InlineData("http://10.0.0.8:3000", false)]
    [InlineData("http://10.0.0.8:3000/myapi", false)]
    [InlineData("foo/bar", true)]
    public void Ctor_Uri_Invalid_Throws(string uriString, bool isRelative)
    {
        Uri endpoint = isRelative ? new(uriString, UriKind.Relative) : new(uriString);
        Action act = () => _ = new RemoteDataset<ClientMovie>(endpoint);
        act.Should().Throw<UriFormatException>();
    }

    [Theory]
    [InlineData("http://localhost/tables/endpoint", "http://localhost/tables/endpoint/")]
    [InlineData("https://foo.azurewebsites.net/tables/endpoint", "https://foo.azurewebsites.net/tables/endpoint/")]
    public void Ctor_Uri_Valid_DoesNotThrow(string uriString, string expectedUri)
    {
        Uri endpoint = new(uriString);
        RemoteDataset<ClientMovie> dataset = new(endpoint);
        dataset.Endpoint.ToString().Should().Be(expectedUri);
        dataset.HttpClient.Should().NotBeNull();
        dataset.EntityContractService.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_UriOptions_Null_Throws()
    {
        RemoteDatasetOptions options = new();
        Action act = () => _ = new RemoteDataset<ClientMovie>(null, options);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("file://localhost/foo", false)]
    [InlineData("http://foo.azurewebsites.net", false)]
    [InlineData("http://foo.azure-api.net", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000/myapi", false)]
    [InlineData("http://10.0.0.8", false)]
    [InlineData("http://10.0.0.8:3000", false)]
    [InlineData("http://10.0.0.8:3000/myapi", false)]
    [InlineData("foo/bar", true)]
    public void Ctor_UriOptions_Invalid_Throws(string uriString, bool isRelative)
    {
        RemoteDatasetOptions options = new();
        Uri endpoint = isRelative ? new(uriString, UriKind.Relative) : new(uriString);
        Action act = () => _ = new RemoteDataset<ClientMovie>(endpoint, options);
        act.Should().Throw<UriFormatException>();
    }

    [Theory]
    [InlineData("http://localhost/tables/endpoint")]
    [InlineData("https://foo.azurewebsites.net/tables/endpoint")]
    public void Ctor_UriOptions_Valid_NullOptions(string uriString)
    {
        Uri endpoint = new(uriString);
        Action act = () => _ = new RemoteDataset<ClientMovie>(endpoint, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_HttpClient_SetsHttpClient()
    {
        Uri endpoint = new("http://localhost/tables/endpoint");
        RemoteDatasetOptions options = new() { HttpClient = new() };
        RemoteDataset<ClientMovie> dataset = new(endpoint, options);
        dataset.HttpClient.Should().BeSameAs(options.HttpClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Ctor_HttpClientFactory_SetsHttpClient_ByName(string clientName)
    {
        Uri endpoint = new("http://localhost/tables/endpoint");
        IDatasyncHttpOptions factoryOptions = new RemoteDatasetOptions { HttpRequestHeaders = new Dictionary<string, string> { { "X-Test", "true" } } };
        RemoteDatasetOptions options = new() { HttpClientFactory = new DatasyncHttpClientFactory(factoryOptions), HttpClientName = clientName };
        HttpClient expected = options.HttpClientFactory.CreateClient("clientmovie");
        RemoteDataset<ClientMovie> dataset = new(endpoint, options);
        dataset.HttpClient.Should().BeSameAs(expected);
    }

    [Fact]
    public void Ctor_HttpClientFactory_WithName_SetsHttpClient_ByName()
    {
        Uri endpoint = new("http://localhost/tables/endpoint");
        IDatasyncHttpOptions factoryOptions = new RemoteDatasetOptions { HttpRequestHeaders = new Dictionary<string, string> { { "X-Test", "true" } } };
        RemoteDatasetOptions options = new() { HttpClientFactory = new DatasyncHttpClientFactory(factoryOptions), HttpClientName = "test" };
        HttpClient expected = options.HttpClientFactory.CreateClient("test");
        RemoteDataset<ClientMovie> dataset = new(endpoint, options);
        dataset.HttpClient.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GenerateExceptionForErrorResponseAsync_ThrowsOnNullResponse()
    {
        HttpResponseMessage response = null;
        RemoteDataset<ClientMovie> dataset = new(new Uri("http://localhost/tables/endpoint"));
        Func<Task> act = async () => await dataset.GenerateExceptionForErrorResponseAsync(response);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadEntityFromResponseAsync_ThrowsOnNullResponse()
    {
        HttpResponseMessage response = null;
        RemoteDataset<ClientMovie> dataset = new(new Uri("http://localhost/tables/endpoint"));
        Func<Task> act = async () => await dataset.ReadEntityFromResponseAsync(response);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadEntityFromResponseAsync_BadJson()
    {
        HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent("not json") };
        RemoteDataset<ClientMovie> dataset = new(new Uri("http://localhost/tables/endpoint"));
        Func<Task> act = async () => await dataset.ReadEntityFromResponseAsync(response);
        await act.Should().ThrowAsync<RemoteDatasetException>();
    }
    #endregion

    #region AddAsync
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
    #endregion

    #region FindAsync
    [Fact]
    public async Task FindAsync_NullId_Throws()
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.FindAsync(null);
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
    public async Task FindAsync_InvalidId_Throws(string id)
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.FindAsync(id);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FindAsync_BasicErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.FindAsync("123");
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == statusCode);
    }

    [Fact]
    public async Task FindAsync_ValidContent_Works()
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"123\"}") });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        T_IdOnly result = await sut.FindAsync("123");

        result.Should().NotBeNull();
        result.Id.Should().Be("123");

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/123"));
        request.Should().HaveHeader("X-Test", "value");
    }

    [Fact]
    public async Task Service_FindAsync_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        ClientMovie result = await dataset.FindAsync(existingMovie.Id);
        result.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Service_FindAsync_MissingId_Returns404()
    {
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.MovieEndpoint), options);

        Func<Task> act = async () => await dataset.FindAsync("missing");
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Service_FindAsync_SoftDeleted_NotDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.SoftDeletedMovieEndpoint), options);

        ClientMovie result = await dataset.FindAsync(existingMovie.Id);
        result.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Service_FindAsync_SoftDeleted_Deleted_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.SoftDeletedMovieEndpoint), options);

        Func<Task> act = async () => await dataset.FindAsync(existingMovie.Id);
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == HttpStatusCode.Gone);
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

    #region ReplaceAsync
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
    #endregion

    #region Test classes
    class T_IdOnly
    {
        public string Id { get; set; }
    }

    class T_IdAndTitle
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    class T_IdAndVersion
    {
        public string Id { get; set; }
        public string Version { get; set; }
    }
    #endregion
}
