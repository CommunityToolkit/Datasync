// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Exceptions;
using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Offline.Operations;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using System.Net;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline.Operations;

[ExcludeFromCodeCoverage]
public class PullOperationManager_Tests : BaseTest
{
    private readonly TestDbContext context = CreateContext();

    private PullOperationManager operationManager { get => this.context.PullOperationManager as PullOperationManager; }

    [Fact]
    public void Ctor_SetsValues()
    {
        operationManager.Should().NotBeNull();

        operationManager.DeltaTokenStore.Should().NotBeNull();
        operationManager.SynchronizableTypes.Should().BeEquivalentTo([typeof(ClientMovie), typeof(Entity3)]);
    }

    #region GetPageAsync(HttpClient, Uri, PageType)
    [Fact]
    public async Task GetPageAsync_Throws_NoItemProperty()
    {
        Type sut = typeof(GPA_E1);

        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);
        Uri requestUri = new("http://localhost/tables/kitchensink");

        Func<Task> act = async () => _ = await operationManager.GetPageAsync(client, requestUri, sut);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    class GPA_E1
    {
        public string NextLink { get; set; }
    }

    [Fact]
    public async Task GetPageAsync_Throws_NoNextLinkProperty()
    {
        Type sut = typeof(GPA_E2);

        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);
        Uri requestUri = new("http://localhost/tables/kitchensink");

        Func<Task> act = async () => _ = await operationManager.GetPageAsync(client, requestUri, sut);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    class GPA_E2
    {
        public IEnumerable<ClientMovie> Items { get; set; }
    }

    [Fact]
    public async Task GetPageAsync_Success_NoNextLink()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);

        Page<ClientMovie> page = CreatePage(5);
        handler.AddResponse(HttpStatusCode.OK, page);

        Page<object> actual = await operationManager.GetPageAsync(client, new Uri("http://localhost/tables/kitchensink"), typeof(Page<ClientMovie>));

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be("http://localhost/tables/kitchensink");

        actual.Should().BeEquivalentTo(page);
    }

    [Fact]
    public async Task GetPageAsync_Success_WithNextLink()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);

        Page<ClientMovie> page = CreatePage(5, null, "$filter=booleanValue&$skip=5");
        handler.AddResponse(HttpStatusCode.OK, page);

        Page<object> actual = await operationManager.GetPageAsync(client, new Uri("http://localhost/tables/kitchensink"), typeof(Page<ClientMovie>));

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be("http://localhost/tables/kitchensink");

        actual.Should().BeEquivalentTo(page);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotModified)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task GetPageAsync_Error(HttpStatusCode code)
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);
        handler.AddResponse(code);
        Uri requestUri = new("http://localhost/tables/movies");

        Func<Task> act = async () => _ = await operationManager.GetPageAsync(client, requestUri, typeof(Page<ClientMovie>));
        DatasyncPullException ex = (await act.Should().ThrowAsync<DatasyncPullException>()).Subject.First();
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.HasContent.Should().BeFalse();
    }

    [Fact]
    public async Task GetPageAsync_SuccessNoContent()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);
        handler.AddResponse(HttpStatusCode.OK);
        Uri requestUri = new("http://localhost/tables/movies");

        Func<Task> act = async () => _ = await operationManager.GetPageAsync(client, requestUri, typeof(Page<ClientMovie>));
        DatasyncPullException ex = (await act.Should().ThrowAsync<DatasyncPullException>()).Subject.First();
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be(200);
        ex.ServiceResponse.IsSuccessful.Should().BeTrue();
        ex.ServiceResponse.HasContent.Should().BeFalse();
    }

    [Fact]
    public async Task GetPageAsync_BadJson()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);
        handler.AddResponseContent("""{bad-json-content""", HttpStatusCode.OK);
        Uri requestUri = new("http://localhost/tables/movies");

        Func<Task> act = async () => _ = await operationManager.GetPageAsync(client, requestUri, typeof(Page<ClientMovie>));
        DatasyncPullException ex = (await act.Should().ThrowAsync<DatasyncPullException>()).Subject.First();
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be(200);
        ex.ServiceResponse.IsSuccessful.Should().BeTrue();
        ex.ServiceResponse.HasContent.Should().BeTrue();
    }

    [Fact]
    public async Task GetPageAsync_NoItems()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler);
        handler.AddResponseContent("""{"nextLink":"abc"}""", HttpStatusCode.OK);
        Uri requestUri = new("http://localhost/tables/movies");

        Page<object> result = await operationManager.GetPageAsync(client, requestUri, typeof(Page<ClientMovie>));
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.NextLink.Should().Be("abc");
    }
    #endregion

    #region PrepareQueryDescription
    [Fact]
    public void PrepareQueryDescription_NQ_NS()
    {
        DatasyncPullQuery<ClientMovie> query = new();
        QueryDescription qd = new QueryTranslator<ClientMovie>(query).Translate();
        string expected = qd.ToODataQueryString();

        DateTimeOffset lastSynchronization = DateTimeOffset.FromUnixTimeSeconds(0L);
        QueryDescription actualQD = PullOperationManager.PrepareQueryDescription(qd, lastSynchronization);

        string actual = Uri.UnescapeDataString(actualQD.ToODataQueryString());
        actual.Should().Be("$orderby=updatedAt&$count=true&__includedeleted=true");
        qd.ToODataQueryString().Should().Be(expected);
    }

    [Fact]
    public void PrepareQueryDescription_Q_NS()
    {
        DatasyncPullQuery<ClientMovie> query = new();
        query.Where(x => x.Rating == TestCommon.Models.MovieRating.G);
        QueryDescription qd = new QueryTranslator<ClientMovie>(query).Translate();
        string expected = qd.ToODataQueryString();

        DateTimeOffset lastSynchronization = DateTimeOffset.FromUnixTimeSeconds(0L);
        QueryDescription actualQD = PullOperationManager.PrepareQueryDescription(qd, lastSynchronization);

        string actual = Uri.UnescapeDataString(actualQD.ToODataQueryString());
        actual.Should().Be("$filter=(rating eq 'G')&$orderby=updatedAt&$count=true&__includedeleted=true");
        qd.ToODataQueryString().Should().Be(expected);
    }

    [Fact]
    public void PrepareQueryDescription_NQ_S()
    {
        DatasyncPullQuery<ClientMovie> query = new();
        QueryDescription qd = new QueryTranslator<ClientMovie>(query).Translate();
        string expected = qd.ToODataQueryString();

        DateTimeOffset lastSynchronization = DateTimeOffset.FromUnixTimeMilliseconds(1724444574291L);
        QueryDescription actualQD = PullOperationManager.PrepareQueryDescription(qd, lastSynchronization);

        string actual = Uri.UnescapeDataString(actualQD.ToODataQueryString());
        actual.Should().Be("$filter=(updatedAt gt cast(2024-08-23T20:22:54.291Z,Edm.DateTimeOffset))&$orderby=updatedAt&$count=true&__includedeleted=true");
        qd.ToODataQueryString().Should().Be(expected);
    }

    [Fact]
    public void PrepareQueryDescription_Q_S()
    {
        DatasyncPullQuery<ClientMovie> query = new();
        query.Where(x => x.Rating == TestCommon.Models.MovieRating.G);
        QueryDescription qd = new QueryTranslator<ClientMovie>(query).Translate();
        string expected = qd.ToODataQueryString();

        DateTimeOffset lastSynchronization = DateTimeOffset.FromUnixTimeMilliseconds(1724444574291L);
        QueryDescription actualQD = PullOperationManager.PrepareQueryDescription(qd, lastSynchronization);

        string actual = Uri.UnescapeDataString(actualQD.ToODataQueryString());
        actual.Should().Be("$filter=((rating eq 'G') and (updatedAt gt cast(2024-08-23T20:22:54.291Z,Edm.DateTimeOffset)))&$orderby=updatedAt&$count=true&__includedeleted=true");
        qd.ToODataQueryString().Should().Be(expected);
    }
    #endregion
}
