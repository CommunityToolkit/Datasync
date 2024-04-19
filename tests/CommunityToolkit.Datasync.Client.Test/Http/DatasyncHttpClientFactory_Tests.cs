// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common.Test.Mocks;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class DatasyncHttpClientFactory_Tests
{
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
