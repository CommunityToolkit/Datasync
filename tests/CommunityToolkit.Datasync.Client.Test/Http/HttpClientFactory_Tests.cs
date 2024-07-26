// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CS0618 // Type or member is obsolete

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using Microsoft.AspNetCore.Http;
using HttpClientFactory = CommunityToolkit.Datasync.Client.Http.HttpClientFactory;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class HttpClientFactory_Tests : Disposable
{
    private readonly MockDelegatingHandler a = new(), b = new();
    private readonly HttpClientHandler c = new();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.a.Dispose();
            this.b.Dispose();
            this.c.Dispose();
        }
    }

    #region CreateClient
    [Fact]
    public void CreateClient_CachesDefaultClient()
    {
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost") };
        HttpClientFactory factory = new(options);
        HttpClient client = factory.CreateClient();
        client.BaseAddress?.ToString().Should().Be("http://localhost/");

        HttpClient cachedClient = factory.CreateClient();
        cachedClient.Should().BeSameAs(client);
    }

    [Theory]
    [InlineData("name")]
    public void CreateClient__CachesNamedClient(string clientName)
    {
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost") };
        HttpClientFactory factory = new(options);
        HttpClient client = factory.CreateClient(clientName);
        client.BaseAddress?.ToString().Should().Be("http://localhost/");

        HttpClient cachedClient = factory.CreateClient(clientName);
        cachedClient.Should().BeSameAs(client);

        HttpClient defaultClient = factory.CreateClient();
        cachedClient.Should().NotBeSameAs(defaultClient);
    }
    #endregion

    #region GenerateClient
    [Fact]
    public void GenerateClient_BasicSetup()
    {
        HttpClientOptions options = new();
        HttpClientFactory factory = new(options);
        HttpClient client = factory.GenerateClient();

        client.BaseAddress.Should().BeNull();
        client.Timeout.TotalSeconds.Should().BeGreaterThan(0);
        client.DefaultRequestHeaders.UserAgent.ToString().Should().StartWith("Datasync/");
        client.DefaultRequestHeaders.Contains("X-ZUMO-INSTALLATION-ID").Should().BeFalse();
        client.DefaultRequestHeaders.GetValues("ZUMO-API-VERSION").Should().ContainSingle("3.0.0");
    }

    [Fact]
    public void GenerateClient_WithEndpoint()
    {
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost") };
        HttpClientFactory factory = new(options);
        HttpClient client = factory.GenerateClient();

        client.BaseAddress?.ToString().Should().Be("http://localhost/");
        client.Timeout.TotalSeconds.Should().BeGreaterThan(0);
        client.DefaultRequestHeaders.UserAgent.ToString().Should().StartWith("Datasync/");
        client.DefaultRequestHeaders.Contains("X-ZUMO-INSTALLATION-ID").Should().BeFalse();
        client.DefaultRequestHeaders.GetValues("ZUMO-API-VERSION").Should().ContainSingle("3.0.0");
    }

    [Fact]
    public void GenerateClient_WithTimeout()
    {
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost"), Timeout = TimeSpan.FromSeconds(30) };
        HttpClientFactory factory = new(options);
        HttpClient client = factory.GenerateClient();

        client.BaseAddress?.ToString().Should().Be("http://localhost/");
        client.Timeout.TotalSeconds.Should().Be(30);
    }

    [Fact]
    public void GenerateClient_WithUserAgent()
    {
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost"), UserAgent = "foo" };
        HttpClientFactory factory = new(options);
        HttpClient client = factory.GenerateClient();

        client.BaseAddress?.ToString().Should().Be("http://localhost/");
        client.DefaultRequestHeaders.UserAgent.ToString().Should().Be("foo");
    }

    [Fact]
    public void GenerateClient_WithInstallationId()
    {
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost"), InstallationId = "test-1234" };
        HttpClientFactory factory = new(options);
        HttpClient client = factory.GenerateClient();

        client.BaseAddress?.ToString().Should().Be("http://localhost/");
        client.DefaultRequestHeaders.GetValues("X-ZUMO-INSTALLATION-ID").Should().ContainSingle("test-1234");
    }

    [Fact]
    public void GenerateClient_WithProtocolVersion()
    {
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost"), ProtocolVersion = "2.0.0" };
        HttpClientFactory factory = new(options);
        HttpClient client = factory.GenerateClient();

        client.BaseAddress?.ToString().Should().Be("http://localhost/");
        client.DefaultRequestHeaders.GetValues("ZUMO-API-VERSION").Should().ContainSingle("2.0.0");
    }
    #endregion

    #region CreatePipeline
    [Fact]
    public void CreatePipeline_NoHandlers_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [] };
        HttpMessageHandler rootHandler = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        rootHandler.Should().BeOfType<HttpClientHandler>().And.NotBeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_C_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.c] };
        HttpMessageHandler rootHandler = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        rootHandler.Should().BeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_B_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.b] };
        HttpMessageHandler rootHandler = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        rootHandler.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeOfType<HttpClientHandler>().And.NotBeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_BC_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.b, this.c] };
        HttpMessageHandler rootHandler = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        rootHandler.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_AB_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.a, this.b] };
        HttpMessageHandler rootHandler = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        rootHandler.Should().BeSameAs(this.a);
        this.a.InnerHandler.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeOfType<HttpClientHandler>().And.NotBeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_ABC_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.a, this.b, this.c] };
        HttpMessageHandler rootHandler = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        rootHandler.Should().BeSameAs(this.a);
        this.a.InnerHandler.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeSameAs(this.c);
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_CB_ThrowsArgumentException()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.c, this.b] };
        Action act = () => _ = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_CAB_ThrowsArgumentException()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.c, this.a, this.b] };
        Action act = () => _ = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    [Trait("Method", "CreatePipeline")]
    public void CreatePipeline_ACB_ThrowsArgumentException()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.a, this.c, this.b] };
        Action act = () => _ = HttpClientFactory.CreatePipeline(options.HttpPipeline);
        act.Should().Throw<ArgumentException>();
    }
    #endregion

    #region IsHttpEndpoint
    [Theory]
    [InlineData("datasync://null", false)]
    [InlineData("file:///", false)]
    [InlineData("http://localhost", true)]
    [InlineData("https://localhsot", true)]
    public void IsHttpEndpoint_Works(string endpoint, bool expected)
    {
        Uri sut = new(endpoint);
        HttpClientFactory.IsHttpEndpoint(sut).Should().Be(expected);
    }
    #endregion
}
