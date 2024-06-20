// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class DefaultHttpClientFactory_Tests : IDisposable
{
    private readonly MockDelegatingHandler a = new(), b = new();
    private readonly HttpClientHandler c = new();

    [Fact]
    public void CreateClient_CreatesCachedClient()
    {
        DefaultHttpClientFactory sut = new(new Uri("http://localhost/"), new HttpClientOptions());

        HttpClient c1 = sut.CreateClient();
        HttpClient c2 = sut.CreateClient();
        c1.Should().NotBeNull().And.BeSameAs(c2);

        HttpClient c3 = sut.CreateClient("datasync");
        HttpClient c4 = sut.CreateClient("datasync");
        c3.Should().NotBeNull().And.BeSameAs(c4).And.NotBeSameAs(c1);

        sut._cache.Count.Should().Be(2);
    }

    [Fact]
    public async Task CreateClient_InstallsHandlers()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.a] };
        DefaultHttpClientFactory sut = new(new Uri("http://localhost/"), options);
        HttpClient c1 = sut.CreateClient();

        this.a.AddResponse(HttpStatusCode.NoContent);

        HttpResponseMessage resp = await c1.DeleteAsync("/tmp/a");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        this.a.Requests.Count.Should().Be(1);
        this.a.Requests[0].Method.Should().Be(HttpMethod.Delete);
        this.a.Requests[0].RequestUri.Should().Be("http://localhost/tmp/a");
    }

    [Fact]
    public void Ctor_WithoutTimeout_SetsDefaultTimeout()
    {
        HttpClientOptions options = new();
        DefaultHttpClientFactory sut = new(new Uri("http://localhost/"), options);

        HttpClient c1 = sut.CreateClient();
        c1.Timeout.TotalMilliseconds.Should().Be(60000);
    }

    [Fact]
    public void Ctor_WithTimeout_SetsTimeout()
    {
        HttpClientOptions options = new() { HttpTimeout = TimeSpan.FromMilliseconds(15000) };
        DefaultHttpClientFactory sut = new(new Uri("http://localhost/"), options);

        HttpClient c1 = sut.CreateClient();
        c1.Timeout.TotalMilliseconds.Should().Be(15000);
    }

    [Fact]
    public void CreatePipeline_NoHandlers_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [] };
        HttpMessageHandler sut = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        sut.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    public void CreatePipeline_C_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.c] };
        HttpMessageHandler sut = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        sut.Should().BeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_B_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.b] };
        HttpMessageHandler sut = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        sut.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    public void CreatePipeline_BC_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.b, this.c] };
        HttpMessageHandler sut = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        sut.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_AB_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.a, this.b] };
        HttpMessageHandler sut = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        sut.Should().BeSameAs(this.a);
        this.a.InnerHandler.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeAssignableTo<HttpClientHandler>();
    }

    [Fact]
    public void CreatePipeline_ABC_CreatesPipeline()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.a, this.b, this.c] };
        HttpMessageHandler sut = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        sut.Should().BeSameAs(this.a);
        this.a.InnerHandler.Should().BeSameAs(this.b);
        this.b.InnerHandler.Should().BeSameAs(this.c);
    }

    [Fact]
    public void CreatePipeline_CB_ThrowsArgumentException()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.c, this.b] };
        Action act = () => _ = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreatePipeline_CAB_ThrowsArgumentException()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.c, this.a, this.b] };
        Action act = () => _ = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreatePipeline_ACB_ThrowsArgumentException()
    {
        HttpClientOptions options = new() { HttpPipeline = [this.a, this.c, this.b] };
        Action act = () => _ = DefaultHttpClientFactory.CreatePipeline(options.HttpPipeline);
        act.Should().Throw<ArgumentException>();
    }

    #region IDisposable
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.a.Dispose();
            this.b.Dispose();
            this.c.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
