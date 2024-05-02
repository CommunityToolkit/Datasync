// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>, IDisposable
{
    #region Test setup
    private readonly MockDelegatingHandler mockHandler = new();

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

    #region Test classes
    class T_IdOnly
    {
        public string Id { get; set; }
    }
    #endregion
}
