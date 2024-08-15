// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using NSubstitute;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class DatasyncOfflineOptionsBuilder_Tests
{
    [Fact]
    public void UseHttpClientFactory()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        BasicHttpClientFactory clientFactory = new(new HttpClient());

        sut.UseHttpClientFactory(clientFactory);
        sut._httpClientFactory.Should().BeSameAs(clientFactory);
    }

    [Fact]
    public void UseHttpClientFactory_Null()
    { 
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        Action act = () => _ = sut.UseHttpClientFactory(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseHttpClient()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        HttpClient client = new();

        sut.UseHttpClient(client);
        HttpClient actual = sut._httpClientFactory.CreateClient();
        actual.Should().BeSameAs(client);
    }

    [Fact]
    public void UseHttpClient_Null()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        Action act = () => _ = sut.UseHttpClient(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseEndpoint()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        Uri endpoint = new("http://localhost");

        sut.UseEndpoint(endpoint);
        HttpClient actual = sut._httpClientFactory.CreateClient();
        actual.BaseAddress.ToString().Should().Be("http://localhost/");
    }

    [Theory, MemberData(nameof(EndpointTestCases.InvalidEndpointTestCases), MemberType = typeof(EndpointTestCases))]
    public void UseEndpoint_Invalid(Uri endpoint)
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);

        Action act = () => _ = sut.UseEndpoint(endpoint);
        act.Should().Throw<UriFormatException>();
    }

    [Fact]
    public void UseHttpClientOptions()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        HttpClientOptions options = new() { Endpoint = new Uri("http://localhost") };

        sut.UseHttpClientOptions(options);
        HttpClient actual = sut._httpClientFactory.CreateClient();
        actual.BaseAddress.ToString().Should().Be("http://localhost/");
    }

    [Fact]
    public void UseHttpClientOptions_Null()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);

        Action act = () => _ = sut.UseHttpClientOptions(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Entity_Generic()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        string typeName = typeof(ClientMovie).FullName;

        _ = sut.Entity<ClientMovie>(options =>
        {
            options.ClientName = "foo";
            options.Endpoint = new Uri("/bar", UriKind.Relative);
        });

        sut._entities[typeName].EntityType.Should().Be(typeof(ClientMovie));
        sut._entities[typeName].ClientName.Should().Be("foo");
        sut._entities[typeName].Endpoint.ToString().Should().Be("/bar");
    }

    [Fact]
    public void Entity_Generic_Unknown()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        string typeName = typeof(ClientMovie).FullName;

        Action act = () => _ = sut.Entity<InMemoryMovie>(options => { /* Do Nothing */ });
        act.Should().Throw<DatasyncException>();
    }

    [Fact]
    public void Entity_Arg()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        string typeName = typeof(ClientMovie).FullName;

        _ = sut.Entity(typeof(ClientMovie), options =>
        {
            options.ClientName = "foo";
            options.Endpoint = new Uri("/bar", UriKind.Relative);
        });

        sut._entities[typeName].EntityType.Should().Be(typeof(ClientMovie));
        sut._entities[typeName].ClientName.Should().Be("foo");
        sut._entities[typeName].Endpoint.ToString().Should().Be("/bar");
    }

    [Fact]
    public void Entity_Arg_Unknown()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        string typeName = typeof(ClientMovie).FullName;

        Action act = () => _ = sut.Entity(typeof(InMemoryMovie), options => { /* Do Nothing */ });
        act.Should().Throw<DatasyncException>();
    }

    [Fact]
    public void GetOfflineOptions_NoClientFactory()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);

        Action act = () => _ = sut.GetOfflineOptions(typeof(ClientMovie));
        act.Should().Throw<DatasyncException>();
    }

    [Fact]
    public void GetOfflineOptions_InvalidEntityType()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        sut.UseEndpoint(new Uri("http://localhost"));

        Action act = () => _ = sut.GetOfflineOptions(typeof(InMemoryMovie));
        act.Should().Throw<DatasyncException>();
    }


    [Fact]
    public void GetOfflineOptions_Defaults()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        HttpClient client = new();

        sut.UseHttpClient(client);
        DatasyncOfflineOptions options = sut.GetOfflineOptions(typeof(ClientMovie));
        options.Endpoint.ToString().Should().Be("/tables/clientmovie");
        options.HttpClient.Should().BeSameAs(client);
    }

    [Fact]
    public void GetOfflineOptions_Sets()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        HttpClient client = new();
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        sut.UseHttpClientFactory(factory).Entity<ClientMovie>(opt =>
        {
            opt.ClientName = "foo";
            opt.Endpoint = new Uri("http://localhost/foo");
        });

        DatasyncOfflineOptions options = sut.GetOfflineOptions(typeof(ClientMovie));
        options.HttpClient.Should().NotBeNull();
        options.Endpoint.ToString().Should().Be("http://localhost/foo");

        factory.Received().CreateClient("foo");
    }
}
