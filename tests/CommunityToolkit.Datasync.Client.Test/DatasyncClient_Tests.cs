// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Common;
using NSubstitute;

namespace CommunityToolkit.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
public class DatasyncClient_Tests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://")]
    [InlineData("file://localhost/foo")]
    [InlineData("http://foo.azurewebsites.net")]
    [InlineData("http://foo.azure-api.net")]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]")]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000")]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000/myapi")]
    [InlineData("http://10.0.0.8")]
    [InlineData("http://10.0.0.8:3000")]
    [InlineData("http://10.0.0.8:3000/myapi")]
    [InlineData("foo/bar")]
    public void CtorString_Invalid_Throws(string endpoint)
    {
        Action act = () => _ = new DatasyncClient(endpoint);
        if (endpoint == null)
        {
            act.Should().Throw<ArgumentNullException>();
        } 
        else
        {
            act.Should().Throw<UriFormatException>();
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", true)]
    [InlineData("http://", false)]
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
    [Trait("Method", "Ctor(Uri)")]
    public void CtorUri_Invalid_Throws(string endpoint, bool isRelative)
    {
        Action act = () =>
        {
            Uri uri = endpoint == null ? null : (isRelative ? new Uri(endpoint, UriKind.Relative) : new Uri(endpoint));
            _ = new DatasyncClient(uri);
        };
        if (endpoint == null)
        {
            act.Should().Throw<ArgumentNullException>();
        }
        else
        {
            act.Should().Throw<UriFormatException>();
        }
    }

    [Fact]
    public void CtorString_NullOptions_Throws()
    {
        const string endpoint = "https://foo.azurewebsites.net/";
        Action act = () => _ = new DatasyncClient(endpoint, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CtorUri_NullOptions_Throws()
    {
        Uri endpoint = new("https://foo.azurewebsites.net/");
        Action act = () => _ = new DatasyncClient(endpoint, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory, ClassData(typeof(EndpointTestCases))]
    public void CtorString_Valid_SetsEndpoint(EndpointTestCase testcase)
    {
        DatasyncClient client = new(testcase.BaseEndpoint);
        client.Endpoint.ToString().Should().Be(testcase.NormalizedEndpoint);
        client.ClientOptions.Should().NotBeNull();
        client.HttpClientFactory.Should().NotBeNull();
    }

    [Theory, ClassData(typeof(EndpointTestCases))]
    public void CtorStringOptions_Valid_SetsEndpoint(EndpointTestCase testcase)
    {
        DatasyncClientOptions options = new();
        DatasyncClient client = new(testcase.BaseEndpoint, options);
        client.Endpoint.ToString().Should().Be(testcase.NormalizedEndpoint);
        client.ClientOptions.Should().BeSameAs(options);
        client.HttpClientFactory.Should().NotBeNull();
    }

    [Theory, ClassData(typeof(EndpointTestCases))]
    public void CtorUri_Valid_SetsEndpoint(EndpointTestCase testcase)
    {
        DatasyncClient client = new(new Uri(testcase.BaseEndpoint));
        client.Endpoint.ToString().Should().Be(testcase.NormalizedEndpoint);
        client.ClientOptions.Should().NotBeNull();
        client.HttpClientFactory.Should().NotBeNull();
    }

    [Theory, ClassData(typeof(EndpointTestCases))]
    public void CtorUriOptions_Valid_SetsEndpoint(EndpointTestCase testcase)
    {
        DatasyncClientOptions options = new();
        DatasyncClient client = new(new Uri(testcase.BaseEndpoint), options);
        client.Endpoint.ToString().Should().Be(testcase.NormalizedEndpoint);
        client.ClientOptions.Should().BeSameAs(options);
        client.HttpClientFactory.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Setting_Options()
    {
        DatasyncClientOptions options = new() 
        {
            DatasyncServiceOptions = Substitute.For<IDatasyncServiceOptions>(),
            HttpClientFactory = Substitute.For<IHttpClientFactory>(),
            HttpClientName = "datasync",
            ParallelOperations = 4,
            TableEndpointResolver = (table) => $"/api/tables/{table}",
            EntityIdGenerator = (_) => Guid.NewGuid().ToString("D")
        };
        DatasyncClient client = new("http://localhost/", options);
        client.ClientOptions.Should().BeSameAs(options);
        client.ServiceOptions.Should().BeSameAs(options.DatasyncServiceOptions);
        client.HttpClientFactory.Should().BeSameAs(options.HttpClientFactory);
        client.Endpoint.Should().Be("http://localhost/");
        client.ClientOptions.ParallelOperations.Should().Be(4);
        client.ClientOptions.HttpClientName.Should().Be("datasync");
    }
}
