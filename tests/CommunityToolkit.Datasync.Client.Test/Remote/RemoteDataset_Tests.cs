// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Remote;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Tests : IDisposable
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

    [Fact]
    public async Task ReadPageOfEntitiesFromResponseAsync_ThrowsOnNullResponse()
    {
        HttpResponseMessage response = null;
        RemoteDataset<ClientMovie> dataset = new(new Uri("http://localhost/tables/endpoint"));
        Func<Task> act = async () => await dataset.ReadPageOfEntitiesFromResponseAsync(response);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadPageOfEntitiesFromResponseAsync_BadJson()
    {
        HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent("not json") };
        RemoteDataset<ClientMovie> dataset = new(new Uri("http://localhost/tables/endpoint"));
        Func<Task> act = async () => await dataset.ReadPageOfEntitiesFromResponseAsync(response);
        await act.Should().ThrowAsync<RemoteDatasetException>();
    }

    [Fact]
    public async Task FuncAsyncPageable_NullReturn_ReturnsEmptySet()
    {
        FuncAsyncPageable<ClientMovie> sut = new(new(_ => ValueTask.FromResult<Page<ClientMovie>>(null)));
        IAsyncEnumerable<Page<ClientMovie>> pages = sut.AsPages();
        Page<ClientMovie> firstPage = await pages.FirstOrDefaultAsync();

        firstPage.Should().NotBeNull();
        firstPage.Items.Should().BeNullOrEmpty();
        firstPage.Count.Should().BeNull();
        firstPage.NextLink.Should().BeNull();
    }

    [Fact]
    public void RemoteDatasetOptions_CanSetSerializerOptions()
    {
        JsonSerializerOptions jso = new(JsonSerializerDefaults.Web);
        RemoteDatasetOptions options = new() { JsonSerializerOptions = jso };
        RemoteDataset<ClientMovie> dataset = new(new Uri("https://localhost/"), options);
        dataset.EntityContractService.JsonSerializerOptions.Should().BeSameAs(jso);
    }
}
