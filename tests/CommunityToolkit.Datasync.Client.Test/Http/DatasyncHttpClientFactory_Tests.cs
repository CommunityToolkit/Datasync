// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common.Test.Mocks;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class DatasyncHttpClientFactory_Tests
{
    [Fact]
    public void CreateClient_SetsHttpTimeout()
    {
        RemoteDatasetOptions options = new() { HttpTimeout = TimeSpan.FromSeconds(30) };
        DatasyncHttpClientFactory factory = new(options);

        HttpClient client = factory.CreateClient("test");
        client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CreateClient_SetsRequestHeaders()
    {
        RemoteDatasetOptions options = new() { HttpRequestHeaders = new Dictionary<string, string> { { "test", "value" } } };
        DatasyncHttpClientFactory factory = new(options);

        HttpClient client = factory.CreateClient("test");
        client.DefaultRequestHeaders.GetValues("test").Should().Contain("value");
    }

    [Fact]
    public void CreateClient_WorksWithPipeline()
    {
        HttpClientHandler c = new();
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions() { HttpPipeline = [c] });

        HttpClient client = factory.CreateClient("test");
        client.Should().NotBeNull();
    }

    [Fact]
    public void CreateClient_NullNameWorks()
    {
        RemoteDatasetOptions options = new() { HttpRequestHeaders = new Dictionary<string, string> { { "test", "value" } } };
        DatasyncHttpClientFactory factory = new(options);

        HttpClient client = factory.CreateClient(null);
        client.DefaultRequestHeaders.GetValues("test").Should().Contain("value");
    }

    [Fact]
    public void CreateClient_WorksWithNullPipeline()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions() { HttpPipeline = null });

        HttpClient client = factory.CreateClient("test");
        client.Should().NotBeNull();
    }

    [Fact]
    public void CreateClient_CachesClient()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());

        HttpClient client1 = factory.CreateClient("test");
        HttpClient client2 = factory.CreateClient("test");

        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void CreateClient_ProducesNamedClients()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());

        HttpClient client1 = factory.CreateClient("test1");
        HttpClient client2 = factory.CreateClient("test2");

        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void CreatePipeline_NoHandlers_CreatesPipeline()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        IEnumerable<HttpMessageHandler> handlers = [];

        HttpMessageHandler rootHandler = factory.CreatePipeline(handlers);

        rootHandler.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    public void CreatePipeline_C_CreatesPipeline()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        HttpClientHandler c = new();
        IEnumerable<HttpMessageHandler> handlers = [c];

        HttpMessageHandler rootHandler = factory.CreatePipeline(handlers);

        rootHandler.Should().BeSameAs(c);
    }

    [Fact]
    public void CreatePipeline_B_CreatesPipeline()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        MockDelegatingHandler b = new();
        IEnumerable<HttpMessageHandler> handlers = [b];

        HttpMessageHandler rootHandler = factory.CreatePipeline(handlers);

        // Assert
        rootHandler.Should().BeSameAs(b);
        b.InnerHandler.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    public void CreatePipeline_BC_CreatesPipeline()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        MockDelegatingHandler b = new();
        HttpClientHandler c = new();
        IEnumerable<HttpMessageHandler> handlers = [b, c];

        HttpMessageHandler rootHandler = factory.CreatePipeline(handlers);

        rootHandler.Should().BeSameAs(b);
        b.InnerHandler.Should().BeSameAs(c);
    }

    [Fact]
    public void CreatePipeline_AB_CreatesPipeline()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        MockDelegatingHandler a = new();
        MockDelegatingHandler b = new();
        IEnumerable<HttpMessageHandler> handlers = [a, b];

        HttpMessageHandler rootHandler = factory.CreatePipeline(handlers);

        rootHandler.Should().BeSameAs(a);
        a.InnerHandler.Should().BeSameAs(b);
        b.InnerHandler.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    public void CreatePipeline_ABC_CreatesPipeline()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        MockDelegatingHandler a = new();
        MockDelegatingHandler b = new();
        HttpClientHandler c = new();
        IEnumerable<HttpMessageHandler> handlers = [a, b, c];

        HttpMessageHandler rootHandler = factory.CreatePipeline(handlers);

        rootHandler.Should().BeSameAs(a);
        a.InnerHandler.Should().BeSameAs(b);
        b.InnerHandler.Should().BeSameAs(c);
    }

    [Fact]
    public void CreatePipeline_CB_ThrowsArgumentException()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        MockDelegatingHandler b = new();
        HttpClientHandler c = new();
        IEnumerable<HttpMessageHandler> handlers = [c, b];

        Assert.Throws<ArgumentException>(() => _ = factory.CreatePipeline(handlers));
    }

    [Fact]
    public void CreatePipeline_CAB_ThrowsArgumentException()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        MockDelegatingHandler a = new();
        MockDelegatingHandler b = new();
        HttpClientHandler c = new();
        IEnumerable<HttpMessageHandler> handlers = [c, a, b];

        Assert.Throws<ArgumentException>(() => _ = factory.CreatePipeline(handlers));
    }

    [Fact]
    public void CreatePipeline_ACB_ThrowsArgumentException()
    {
        DatasyncHttpClientFactory factory = new(new RemoteDatasetOptions());
        MockDelegatingHandler a = new();
        MockDelegatingHandler b = new();
        HttpClientHandler c = new();
        IEnumerable<HttpMessageHandler> handlers = [a, c, b];

        Assert.Throws<ArgumentException>(() => _ = factory.CreatePipeline(handlers));
    }
}
