// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common.Test.Mocks;

// We use IHttpClientFactory in some tests to ensure we are calling the right methods.
#pragma warning disable CA1859 // Use concrete types when possible for improved performance

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class DatasyncHttpClientFactory_Tests
{
    #region CreateClient
    [Theory]
    [InlineData("")]
    [InlineData("default")]
    [InlineData("test")]
    public void CreateClient_Works(string name)
    {
        IHttpClientFactory factory = new DatasyncHttpClientFactory(new DatasyncHttpClientOptions { BaseAddress = new("http://localhost/") });
        HttpClient client = factory.CreateClient(name);
        client.BaseAddress.ToString().Should().Be("http://localhost/");
    }

    [Theory]
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
    public void CreateClient_Throws(string endpoint, bool isRelative = false)
    {
        Uri sut = isRelative ? new Uri(endpoint, UriKind.Relative) : new Uri(endpoint);
        DatasyncHttpClientOptions options = new() { BaseAddress = sut };
        IHttpClientFactory factory = new DatasyncHttpClientFactory(options);
        Assert.Throws<UriFormatException>(() => factory.CreateClient("name"));
    }

    [Fact]
    public void CreateClient_ReturnsCachedCopy()
    {
        DatasyncHttpClientOptions options = new() { BaseAddress = new("https://localhost/") };
        IHttpClientFactory factory = new DatasyncHttpClientFactory(options);

        HttpClient client1 = factory.CreateClient("name");
        HttpClient client2 = factory.CreateClient("name");
        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void CreateClient_CreatesDifferentCopies()
    {
        DatasyncHttpClientOptions options = new() { BaseAddress = new("https://localhost/") };
        IHttpClientFactory factory = new DatasyncHttpClientFactory(options);

        HttpClient client1 = factory.CreateClient("name1");
        HttpClient client2 = factory.CreateClient("name2");
        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void CreateClient_SetsTimeout()
    {
        DatasyncHttpClientOptions options = new() { BaseAddress = new("https://localhost/"), HttpTimeout = TimeSpan.FromSeconds(10) };
        IHttpClientFactory factory = new DatasyncHttpClientFactory(options);

        HttpClient client = factory.CreateClient("name");
        client.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateClient_SetsHeaders()
    {
        DatasyncHttpClientOptions options = new() { BaseAddress = new("https://localhost/"), HttpRequestHeaders = new Dictionary<string, string> { ["X-Test"] = "Test" } };
        IHttpClientFactory factory = new DatasyncHttpClientFactory(options);

        HttpClient client = factory.CreateClient("name");
        client.DefaultRequestHeaders.GetValues("X-Test").Should().Contain("Test");
    }
    #endregion

    #region CreatePipeline
    private static DatasyncHttpClientFactory CreateFactoryWithHandlers(IEnumerable<HttpMessageHandler> handlers)
    {
        IDatasyncHttpClientOptions options = new DatasyncHttpClientOptions { HttpPipeline = handlers };
        return new DatasyncHttpClientFactory(options);
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_NoHandlers_CreatesPipeline()
    {
        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([]);
        HttpMessageHandler pipeline = factory.CreatePipeline();

        pipeline.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_C_CreatesPipeline()
    {
        HttpClientHandler c = new();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([c]);
        HttpMessageHandler pipeline = factory.CreatePipeline();

        pipeline.Should().BeSameAs(c);
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_B_CreatesPipeline()
    {
        DelegatingHandler b = new MockDelegatingHandler();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([b]);
        HttpMessageHandler pipeline = factory.CreatePipeline();

        pipeline.Should().BeSameAs(b);
        (pipeline as DelegatingHandler).InnerHandler.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_BC_CreatesPipeline()
    {
        DelegatingHandler b = new MockDelegatingHandler();
        HttpClientHandler c = new();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([b, c]);
        HttpMessageHandler pipeline = factory.CreatePipeline();

        pipeline.Should().BeSameAs(b);
        (pipeline as DelegatingHandler).InnerHandler.Should().BeSameAs(c);
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_AB_CreatesPipeline()
    {
        DelegatingHandler a = new MockDelegatingHandler();
        DelegatingHandler b = new MockDelegatingHandler();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([a, b]);
        HttpMessageHandler pipeline = factory.CreatePipeline();

        pipeline.Should().BeSameAs(a);
        (pipeline as DelegatingHandler).InnerHandler.Should().BeSameAs(b);
        ((pipeline as DelegatingHandler).InnerHandler as DelegatingHandler).InnerHandler.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_ABC_CreatesPipeline()
    {
        DelegatingHandler a = new MockDelegatingHandler();
        DelegatingHandler b = new MockDelegatingHandler();
        HttpClientHandler c = new();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([a, b, c]);
        HttpMessageHandler pipeline = factory.CreatePipeline();

        pipeline.Should().BeSameAs(a);
        (pipeline as DelegatingHandler).InnerHandler.Should().BeSameAs(b);
        ((pipeline as DelegatingHandler).InnerHandler as DelegatingHandler).InnerHandler.Should().BeSameAs(c);
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_CB_Throws()
    {
        DelegatingHandler b = new MockDelegatingHandler();
        HttpClientHandler c = new();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([c, b]);
        Action act = () => _ = factory.CreatePipeline();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_CAB_ThrowsArgumentException()
    {
        DelegatingHandler a = new MockDelegatingHandler();
        DelegatingHandler b = new MockDelegatingHandler();
        HttpClientHandler c = new();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([c, a, b]);
        Action act = () => _ = factory.CreatePipeline();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_ACB_ThrowsArgumentException()
    {
        DelegatingHandler a = new MockDelegatingHandler();
        DelegatingHandler b = new MockDelegatingHandler();
        HttpClientHandler c = new();

        DatasyncHttpClientFactory factory = CreateFactoryWithHandlers([a, c, b]);
        Action act = () => _ = factory.CreatePipeline();
        act.Should().Throw<InvalidOperationException>();
    }
    #endregion
}
