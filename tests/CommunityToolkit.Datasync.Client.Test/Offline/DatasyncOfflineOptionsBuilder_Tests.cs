// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using NSubstitute;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class DatasyncOfflineOptionsBuilder_Tests : BaseTest
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
    public void Build_NoClientFactory()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);

        Action act = () => _ = sut.Build();
        act.Should().Throw<DatasyncException>();
    }

    [Fact]
    public void Build_Defaults()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        HttpClient client = new();

        sut.UseHttpClient(client);
        OfflineOptions options = sut.Build();
        EntityDatasyncOptions result = options.GetOptions(typeof(ClientMovie));
        result.Endpoint.ToString().Should().Be("/tables/clientmovie");
        result.HttpClient.Should().BeSameAs(client);
    }

    [Fact]
    public void Build_Sets()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        HttpClient client = new();
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        sut.UseHttpClientFactory(factory);
        sut.Entity<ClientMovie>(opt =>
        {
            opt.ClientName = "foo";
            opt.Endpoint = new Uri("http://localhost/foo");
        });
        sut.Entity<ClientKitchenSink>(opt =>
        {
            opt.Query.Where(x => x.StringValue == "abc");
        });

        OfflineOptions options = sut.Build();
        EntityDatasyncOptions result = options.GetOptions(typeof(ClientMovie));
        result.HttpClient.Should().NotBeNull();
        result.Endpoint.ToString().Should().Be("http://localhost/foo");
        result.QueryDescription.ToODataQueryString().Should().Be("");

        EntityDatasyncOptions result2 = options.GetOptions(typeof(ClientKitchenSink));
        result2.QueryDescription.ToODataQueryString().Should().Be("$filter=%28stringValue%20eq%20%27abc%27%29");

        factory.Received().CreateClient("foo");
    }

    [Fact]
    public void Entity_SetsType()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);

        sut.Entity(typeof(ClientMovie), cfg =>
        {
            cfg.EntityType.Should().Be(typeof(ClientMovie));
        });

        sut.Entity<ClientMovie>(cfg =>
        {
            cfg.EntityType.Should().Be(typeof(ClientMovie));
        });
    }

    [Fact]
    public void OfflineOptions_Defaults()
    {
        Type[] entityTypes = [typeof(ClientMovie), typeof(ClientKitchenSink)];
        DatasyncOfflineOptionsBuilder sut = new(entityTypes);
        HttpClient client = new();

        sut.UseHttpClient(client);
        OfflineOptions options = sut.Build();
        EntityDatasyncOptions result = options.GetOptions(typeof(Entity3));
        result.HttpClient.Should().NotBeNull();
        result.Endpoint.ToString().Should().Be("/tables/entity3");
        result.QueryDescription.ToODataQueryString().Should().Be("");
    }
}
