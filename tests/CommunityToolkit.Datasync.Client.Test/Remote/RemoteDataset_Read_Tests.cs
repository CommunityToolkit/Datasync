// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// There are a bunch of Roslynator "simplify the boolean expression" warnings that are disabled here because we want to actually test dumb expressions.
#pragma warning disable RCS1033 // Remove redundant boolean literal
#pragma warning disable RCS1049 // Simplify boolean comparison
#pragma warning disable RCS1068 // Simplify logical negation
#pragma warning disable RCS1155 // Use StringComparison when comparing strings

using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Client.Remote;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Read_Tests : ServiceTest, IClassFixture<ServiceApplicationFactory>, IDisposable
{
    #region Test Setup
    private readonly MockDelegatingHandler mockHandler = new();
    private readonly MediaTypeHeaderValue jsonMediaTypeHeaderValue = new("application/json");
    private readonly RemoteDatasetOptions remoteDatasetOptions;
    private readonly RemoteDataset<ClientMovie> movieDataset;

    public RemoteDataset_Read_Tests(ServiceApplicationFactory factory) : base(factory)
    {
        this.remoteDatasetOptions = new() { HttpClient = this.client };
        this.movieDataset = new(new Uri(this.factory.MovieEndpoint), this.remoteDatasetOptions);
    }

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

    private Page<T_IdOnly> CreatePageOfItems(int count, long? totalCount = null, string nextLink = null)
    {
        List<T_IdOnly> items = [];

        for (int i = 0; i < count; i++)
        {
            items.Add(new T_IdOnly { Id = Guid.NewGuid().ToString("N") });
        }

        Page<T_IdOnly> page = new() { Items = items, Count = totalCount, NextLink = nextLink };
        this.mockHandler.AddResponse(HttpStatusCode.OK, page);
        return page;
    }

    private async Task RunLinqTest(Func<IODataQuery<ClientMovie>, IODataQuery<ClientMovie>> linqExpression, int resultCount, string[] expectedIds)
    {
        DateTimeOffset startTime = DateTimeOffset.Now;
        IODataQuery<ClientMovie> query = this.movieDataset.AsQueryable();

        List<ClientMovie> list = await linqExpression.Invoke(query).ToAsyncEnumerable().ToListAsync();

        list.Count.Should().Be(resultCount);
        string[] actualIds = list.Take(expectedIds.Length).Select(m => m.Id).ToArray();
        actualIds.Should().BeEquivalentTo(expectedIds);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test suite")]
    private async Task RunEnumerableTest(IAsyncEnumerable<ClientMovie> enumerable, int resultCount, string[] expectedIds)
    {
        DateTimeOffset startTime = DateTimeOffset.Now;

        List<ClientMovie> list = await enumerable.ToListAsync();

        list.Count.Should().Be(resultCount);
        string[] actualIds = list.Take(expectedIds.Length).Select(m => m.Id).ToArray();
        actualIds.Should().BeEquivalentTo(expectedIds);
    }

    private Task RunEnumerableTest(IODataQuery<ClientMovie> query, int resultCount, string[] expectedIds)
        => RunEnumerableTest(query.ToAsyncEnumerable(), resultCount, expectedIds);
    #endregion

    #region FindAsync
    [Fact]
    public async Task FindAsync_NullId_Throws()
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.FindAsync(null);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("abcdef gh")]
    [InlineData("?")]
    [InlineData(";")]
    [InlineData("{EA235ADF-9F38-44EA-8DA4-EF3D24755767}")]
    [InlineData("###")]
    [InlineData("!!!")]
    public async Task FindAsync_InvalidId_Throws(string id)
    {
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.FindAsync(id);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FindAsync_BasicErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.FindAsync("123");
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == statusCode);
    }

    [Fact]
    public async Task FindAsync_ValidContent_Works()
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"123\"}") });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        T_IdOnly result = await sut.FindAsync("123");

        result.Should().NotBeNull();
        result.Id.Should().Be("123");

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/123"));
        request.Should().HaveHeader("X-Test", "value");
    }

    [Fact]
    public async Task Service_FindAsync_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie result = await this.movieDataset.FindAsync(existingMovie.Id);
        result.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Service_FindAsync_MissingId_Returns404()
    {
        Func<Task> act = async () => await this.movieDataset.FindAsync("missing");
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Service_FindAsync_SoftDeleted_NotDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.SoftDeletedMovieEndpoint), options);

        ClientMovie result = await dataset.FindAsync(existingMovie.Id);
        result.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Service_FindAsync_SoftDeleted_Deleted_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        RemoteDatasetOptions options = new() { HttpClient = this.client };
        RemoteDataset<ClientMovie> dataset = new(new Uri(this.factory.SoftDeletedMovieEndpoint), options);

        Func<Task> act = async () => await dataset.FindAsync(existingMovie.Id);
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == HttpStatusCode.Gone);
    }
    #endregion

    #region GetNextPageAsync
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("$filter=Year eq 1900&$count=true")]
    [Trait("Method", "GetNextPageAsync")]
    public async Task GetNextPageAsync_ConstructsRequest_WithQuery(string query)
    {
        this.mockHandler.AddResponse(HttpStatusCode.OK, new Page<T_IdOnly>() { Items = [] });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        string expectedUri = string.IsNullOrEmpty(query) ? "http://localhost/tables/endpoint/" : $"http://localhost/tables/endpoint/?{query}";

        _ = await sut.GetNextPageAsync(query, null);

        this.mockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri(expectedUri));
    }

    [Theory]
    [InlineData("https://localhost/tables/foo/?$count=true")]
    [InlineData("https://localhost/tables/foo/?$count=true&$skip=5&$top=10")]
    [InlineData("https://localhost/tables/foo/?$count=true&$skip=5&$top=10&__includedeleted=true")]
    [Trait("Method", "GetNextPageAsync")]
    public async Task GetNextPageAsync_ConstructsRequest_WithRequestUri(string requestUri)
    {
        this.mockHandler.AddResponse(HttpStatusCode.OK, new Page<T_IdOnly>() { Items = [] });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        _ = await sut.GetNextPageAsync(null, requestUri);

        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri(requestUri));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("$filter=Year eq 1900&$count=true")]
    [Trait("Method", "GetNextPageAsync")]
    public async Task GetNextPageAsync_ConstructsRequest_PrefersRequestUri(string query)
    {
        this.mockHandler.AddResponse(HttpStatusCode.OK, new Page<T_IdOnly>() { Items = [] });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        const string requestUri = "https://localhost/tables/foo?$count=true&$skip=5&$top=10&__includedeleted=true";

        _ = await sut.GetNextPageAsync(query, requestUri);

        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri(requestUri));
    }

    [Fact]
    public async Task GetNextPageAsync_ReturnsData()
    {
        const string requestUri = "https://localhost/tables/foo?$count=true&$skip=5&$top=10&__includedeleted=true";
        Page<T_IdOnly> data = new() { Items = [new T_IdOnly { Id = "1" }, new T_IdOnly { Id = "2" }], Count = 42, NextLink = requestUri };
        this.mockHandler.AddResponse(HttpStatusCode.OK, data);
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        Page<T_IdOnly> result = await sut.GetNextPageAsync("", requestUri);

        result.Should().NotBeNull().And.BeEquivalentTo(data);
    }
    #endregion

    #region LongCountAsync
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task LongCountAsync_BasicErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);
        Func<Task> act = async () => await sut.LongCountAsync();
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == statusCode);
    }

    [Fact]
    public async Task LongCountAsync_ReturnsValue()
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"items\":[],\"count\":42}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        long result = await sut.LongCountAsync();
        result.Should().Be(42L);

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/?$count=true&$top=0"));
        request.Should().HaveHeader("X-Test", "value");
    }

    [Fact]
    public async Task LongCountAsync_NoCount_Throws()
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"items\":[]}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        Func<Task> act = async () => _ = await sut.LongCountAsync();
        await act.Should().ThrowAsync<RemoteDatasetException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("$top=20")]
    [InlineData("$skip=20")]
    [InlineData("$count=true")]
    public async Task LongCountAsync_WithQuery_ReturnsValue(string query)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"items\":[],\"count\":42}", this.jsonMediaTypeHeaderValue) });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        long result = await sut.LongCountAsync(query);
        result.Should().Be(42L);

        // Check that the request was constructed correctly
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/?$count=true&$top=0"));
        request.Should().HaveHeader("X-Test", "value");
    }

    [Theory]
    [InlineData("", 248)]
    [InlineData("$filter=((year gt 2000) or (year lt 1940))", 78)]
    public async Task Service_LongCountAsync_ReturnsValue(string query, long expected)
    {
        long actual = await this.movieDataset.LongCountAsync(query);
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task Service_LongCountAsync_WithLinq_ReturnsValue()
    {
        long actual = await this.movieDataset.Where(x => x.Rating == MovieRating.R).LongCountAsync();
        actual.Should().Be(95);
    }
    #endregion

    #region Query
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Query_BasicErrorResponse_Throws(HttpStatusCode statusCode)
    {
        this.mockHandler.Responses.Add(new HttpResponseMessage(statusCode));
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        IAsyncEnumerator<T_IdOnly> enumerator = sut.Query("").GetAsyncEnumerator();

        Func<Task> act = async () => await enumerator.MoveNextAsync();
        (await act.Should().ThrowAsync<RemoteDatasetException>()).Where(e => e.StatusCode == statusCode);
    }

    [Fact]
    public async Task Query_NoItems()
    {
        this.mockHandler.AddResponse(HttpStatusCode.OK, new Page<T_IdOnly>() { Items = [] });
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        IAsyncEnumerator<T_IdOnly> enumerator = sut.Query("").GetAsyncEnumerator();
        bool hasMore = await enumerator.MoveNextAsync();

        hasMore.Should().BeFalse();
        this.mockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/"));
        request.Should().HaveHeader("X-Test", "value");
    }

    [Fact]
    public async Task Query_OnePageOfItems()
    {
        Page<T_IdOnly> page = CreatePageOfItems(5);
        List<T_IdOnly> expectedItems = [.. page.Items];
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        List<T_IdOnly> items = await sut.Query("").ToListAsync();

        this.mockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = this.mockHandler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/"));
        request.Should().HaveHeader("X-Test", "value");

        items.Should().BeEquivalentTo(expectedItems, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Query_TwoPagesOfItems()
    {
        Page<T_IdOnly> page1 = CreatePageOfItems(5, null, "page=2");
        Page<T_IdOnly> page2 = CreatePageOfItems(5);
        List<T_IdOnly> expectedItems = [.. page1.Items, .. page2.Items];
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        List<T_IdOnly> items = await sut.Query("").ToListAsync();

        this.mockHandler.Requests.Should().HaveCount(2);
        HttpRequestMessage request1 = this.mockHandler.Requests[0];
        request1.Method.Should().Be(HttpMethod.Get);
        request1.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/"));
        request1.Should().HaveHeader("X-Test", "value");

        HttpRequestMessage request2 = this.mockHandler.Requests[1];
        request2.Method.Should().Be(HttpMethod.Get);
        request2.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/?page=2"));
        request2.Should().HaveHeader("X-Test", "value");

        items.Should().BeEquivalentTo(expectedItems, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Query_ThreePagesOfItems()
    {
        Page<T_IdOnly> page1 = CreatePageOfItems(5, null, "page=2");
        Page<T_IdOnly> page2 = CreatePageOfItems(5, null, "page=3");
        Page<T_IdOnly> page3 = CreatePageOfItems(5);
        List<T_IdOnly> expectedItems = [.. page1.Items, .. page2.Items, .. page3.Items];
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        List<T_IdOnly> items = await sut.Query("").ToListAsync();

        this.mockHandler.Requests.Should().HaveCount(3);
        HttpRequestMessage request1 = this.mockHandler.Requests[0];
        request1.Method.Should().Be(HttpMethod.Get);
        request1.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/"));
        request1.Should().HaveHeader("X-Test", "value");

        HttpRequestMessage request2 = this.mockHandler.Requests[1];
        request2.Method.Should().Be(HttpMethod.Get);
        request2.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/?page=2"));
        request2.Should().HaveHeader("X-Test", "value");

        HttpRequestMessage request3 = this.mockHandler.Requests[2];
        request3.Method.Should().Be(HttpMethod.Get);
        request3.RequestUri.Should().Be(new Uri("http://localhost/tables/endpoint/?page=3"));
        request3.Should().HaveHeader("X-Test", "value");

        items.Should().BeEquivalentTo(expectedItems, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Query_SetsCount()
    {
        _ = CreatePageOfItems(5, 42);
        RemoteDatasetOptions mockOptions = new() { HttpPipeline = [this.mockHandler], HttpRequestHeaders = new Dictionary<string, string>() { { "X-Test", "value" } } };
        RemoteDataset<T_IdOnly> sut = new(new Uri("http://localhost/tables/endpoint"), mockOptions);

        AsyncPageable<T_IdOnly> pageable = sut.Query("") as AsyncPageable<T_IdOnly>;
        await pageable.GetAsyncEnumerator().MoveNextAsync();

        pageable.Count.Should().Be(42);
    }
    #endregion

    #region LINQ tests against Remote Database
    [Fact]
    public async Task Linq_001()
    {
        await RunLinqTest(
            m => m,
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_002()
    {
        await RunLinqTest(
            m => m.IncludeTotalCount(),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_003()
    {
        await RunLinqTest(
            m => m.IncludeTotalCount(true),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_004()
    {
        await RunLinqTest(
            m => m.IncludeTotalCount(false),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_005()
    {
        await RunLinqTest(
            m => m.IncludeDeletedItems(),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_006()
    {
        await RunLinqTest(
            m => m.IncludeDeletedItems(true),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_007()
    {
        await RunLinqTest(
            m => m.IncludeDeletedItems(false),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_008()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_009()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.Duration),
            Movies.MovieList.Length,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Linq_010()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Linq_011()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.Title),
            Movies.MovieList.Length,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Linq_012()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Linq_013()
    {
        await RunLinqTest(
            m => m.OrderByDescending(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_014()
    {
        await RunLinqTest(
            m => m.OrderByDescending(x => x.Duration),
            Movies.MovieList.Length,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Linq_015()
    {
        await RunLinqTest(
            m => m.OrderByDescending(x => x.Rating),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Linq_016()
    {
        await RunLinqTest(
            m => m.OrderByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Linq_017()
    {
        await RunLinqTest(
            m => m.OrderByDescending(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Linq_018()
    {
        await RunLinqTest(
            m => m.ThenBy(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_019()
    {
        await RunLinqTest(
            m => m.ThenBy(x => x.Duration),
            Movies.MovieList.Length,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Linq_020()
    {
        await RunLinqTest(
            m => m.ThenBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Linq_021()
    {
        await RunLinqTest(
            m => m.ThenBy(x => x.Title),
            Movies.MovieList.Length,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Linq_022()
    {
        await RunLinqTest(
            m => m.ThenBy(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Linq_023()
    {
        await RunLinqTest(
            m => m.ThenByDescending(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_024()
    {
        await RunLinqTest(
            m => m.ThenByDescending(x => x.Duration),
            Movies.MovieList.Length,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Linq_025()
    {
        await RunLinqTest(
            m => m.ThenByDescending(x => x.Rating),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Linq_026()
    {
        await RunLinqTest(
            m => m.ThenByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Linq_027()
    {
        await RunLinqTest(
            m => m.ThenByDescending(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Linq_028()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.Year).ThenBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Linq_029()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.Year).ThenByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Linq_030()
    {
        await RunLinqTest(
            m => m.OrderByDescending(x => x.Year).ThenBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-033", "id-122", "id-188", "id-102", "id-149"]
        );
    }

    [Fact]
    public async Task Linq_031()
    {
        await RunLinqTest(
            m => m.OrderByDescending(x => x.Rating).ThenByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-107", "id-160", "id-092", "id-176", "id-147"]
        );
    }

    [Fact]
    public async Task Linq_032()
    {
        await RunLinqTest(
            m => m.OrderBy(x => x.UpdatedAt),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_033()
    {
        await RunLinqTest(
            m => m.Skip(100),
            Movies.MovieList.Length - 100,
            ["id-100", "id-101", "id-102", "id-103", "id-104"]
        );
    }

    [Fact]
    public async Task Linq_034()
    {
        await RunLinqTest(
            m => m.Skip(200),
            Movies.MovieList.Length - 200,
            ["id-200", "id-201", "id-202", "id-203", "id-204"]
        );
    }

    [Fact]
    public async Task Linq_035()
    {
        await RunLinqTest(
            m => m.Take(100),
            100,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_036()
    {
        await RunLinqTest(
            m => m.Take(200),
            200,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_037()
    {
        await RunLinqTest(
            m => m.Skip(100).Take(50),
            50,
            ["id-100", "id-101", "id-102", "id-103", "id-104"]
        );
    }

    [Fact]
    public async Task Linq_038()
    {
        await RunLinqTest(
            m => m.Skip(200).Take(25),
            25,
            ["id-200", "id-201", "id-202", "id-203", "id-204"]
        );
    }

    [Fact]
    public async Task Linq_101()
    {
        await RunLinqTest(
            m => m.Where(x => x.BestPictureWinner),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_102()
    {
        await RunLinqTest(
            m => m.Where(x => !x.BestPictureWinner),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_103()
    {
        await RunLinqTest(
            m => m.Where(x => x.BestPictureWinner == true),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_104()
    {
        await RunLinqTest(
            m => m.Where(x => x.BestPictureWinner == false),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_105()
    {
        await RunLinqTest(
            m => m.Where(x => x.BestPictureWinner != true),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_106()
    {
        await RunLinqTest(
            m => m.Where(x => x.BestPictureWinner != false),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_107()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.BestPictureWinner == true)),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_108()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.BestPictureWinner != true)),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_109()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.BestPictureWinner == false)),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_110()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.BestPictureWinner != false)),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_111()
    {
        await RunLinqTest(
            m => m.Where(x => x.Duration == 100),
            3,
            ["id-116", "id-159", "id-186"]
        );
    }

    [Fact]
    public async Task Linq_112()
    {
        await RunLinqTest(
            m => m.Where(x => x.Duration < 100),
            44,
            ["id-005", "id-037", "id-041", "id-044", "id-054"]
        );
    }

    [Fact]
    public async Task Linq_113()
    {
        await RunLinqTest(
            m => m.Where(x => x.Duration <= 100),
            47,
            ["id-005", "id-037", "id-041", "id-044", "id-054"]
        );
    }

    [Fact]
    public async Task Linq_114()
    {
        await RunLinqTest(
            m => m.Where(x => x.Duration > 90),
            227,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_115()
    {
        await RunLinqTest(
            m => m.Where(x => x.Duration >= 90),
            227,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_116()
    {
        await RunLinqTest(
            m => m.Where(x => x.Duration != 100),
            245,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_117()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.Duration == 100)),
            245,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_118()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.Duration < 100)),
            204,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_119()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.Duration <= 100)),
            201,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_120()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.Duration > 90)),
            21,
            ["id-041", "id-044", "id-054", "id-079", "id-089"]
        );
    }

    [Fact]
    public async Task Linq_121()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.Duration >= 90)),
            21,
            ["id-041", "id-044", "id-054", "id-079", "id-089"]
        );
    }

    [Fact]
    public async Task Linq_122()
    {
        await RunLinqTest(
            m => m.Where(x => !(x.Duration != 100)),
            3,
            ["id-116", "id-159", "id-186"]
        );
    }

    [Fact]
    public async Task Linq_123()
    {
        DateOnly dt = new(1994, 10, 14);
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate == dt),
            2,
            ["id-000", "id-003"]
        );
    }

    [Fact]
    public async Task Linq_124()
    {
        DateOnly dt = new(1999, 12, 31);
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate > dt),
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Linq_125()
    {
        DateOnly dt = new(1999, 12, 31);
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate >= dt),
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Linq_126()
    {
        DateOnly dt = new(2000, 1, 1);
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate < dt),
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_127()
    {
        DateOnly dt = new(2000, 1, 1);
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate <= dt),
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_128()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title == "The Godfather"),
            1,
            ["id-001"]
        );
    }

    [Fact]
    public async Task Linq_129()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title != "The Godfather"),
            247,
            ["id-000", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task Linq_130()
    {
        await RunLinqTest(
            m => m.Where(x => x.Year > 1929 && x.Year < 1940),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Linq_131()
    {
        await RunLinqTest(
            m => m.Where(x => x.Year >= 1930 && x.Year <= 1939),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Linq_132()
    {
        await RunLinqTest(
            m => m.Where(x => x.Year > 2000 || x.Year < 1940),
            78,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Linq_133()
    {
        await RunLinqTest(
            m => m.Where(x => x.Year > 2000 || !x.BestPictureWinner),
            218,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_134()
    {
        await RunLinqTest(
            m => m.Where(x => x.Year > 1929).Where(x => x.Year < 1940),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Linq_135()
    {
        await RunLinqTest(
            m => m.Where(x => x.Year >= 1930).Where(x => x.Year <= 1939),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Linq_136()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Year >= 1930 && x.Year <= 1940) || (x.Year >= 1950 && x.Year <= 1960)),
            46,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [Fact]
    public async Task Linq_137()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Year - 1900) > 80),
            134,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [Fact]
    public async Task Linq_138()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Year + x.Duration) < 2100),
            103,
            ["id-005", "id-015", "id-016", "id-024", "id-026"]
        );
    }

    [Fact]
    public async Task Linq_139()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Year - 1900) < x.Duration),
            230,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_140()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Duration * 2) < 180),
            21,
            ["id-041", "id-044", "id-054", "id-079", "id-089"]
        );
    }

    [Fact]
    public async Task Linq_141()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Year / 1000.5) == 2),
            6,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [Fact]
    public async Task Linq_142()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Duration % 2) == 1),
            124,
            ["id-001", "id-004", "id-007", "id-008", "id-009"]
        );
    }

    [Fact]
    public async Task Linq_143()
    {
        await RunLinqTest(
            m => m.Where(x => (x.Year - 1900) >= 80 && (x.Year + 10) <= 2000 && x.Duration <= 120),
            13,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [Fact]
    public async Task Linq_144()
    {
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate.Day == 1),
            7,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [Fact]
    public async Task Linq_145()
    {
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate.Month == 11),
            14,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [Fact]
    public async Task Linq_146()
    {
        await RunLinqTest(
            m => m.Where(x => x.ReleaseDate.Year != x.Year),
            52,
            ["id-004", "id-016", "id-024", "id-027", "id-029"]
        );
    }

    [Fact]
    public async Task Linq_147()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.EndsWith("er")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Linq_148()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.ToLower().EndsWith("er")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Linq_149()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.ToUpper().EndsWith("ER")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Linq_150()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.Trim().Length > 10),
            178,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_151()
    {
        await RunLinqTest(
            m => m.Where(x => Math.Round(x.Duration / 60.0) == 2),
            186,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_152()
    {
        await RunLinqTest(
            m => m.Where(x => Math.Ceiling(x.Duration / 60.0) == 2),
            124,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [Fact]
    public async Task Linq_153()
    {
        await RunLinqTest(
            m => m.Where(x => Math.Floor(x.Duration / 60.0) == 2),
            120,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Linq_154()
    {
        await RunLinqTest(
            m => m.Where(x => !x.BestPictureWinner).Where(x => Math.Round(x.Duration / 60.0) == 2),
            162,
            ["id-000", "id-005", "id-009", "id-010", "id-013"]
        );
    }

    [Fact]
    public async Task Linq_155()
    {
        await RunLinqTest(
            m => m.Where(x => !x.BestPictureWinner).Where(x => Math.Ceiling(x.Duration / 60.0) == 2),
            113,
            ["id-005", "id-025", "id-026", "id-027", "id-028"]
        );
    }

    [Fact]
    public async Task Linq_156()
    {
        await RunLinqTest(
            m => m.Where(x => !x.BestPictureWinner).Where(x => Math.Floor(x.Duration / 60.0) == 2),
            99,
            ["id-000", "id-003", "id-004", "id-006", "id-009"]
        );
    }

    [Fact]
    public async Task Linq_157()
    {
        await RunLinqTest(
            m => m.Where(x => Math.Round(x.Duration / 60.0) == (float)2),
            186,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Linq_158()
    {
        await RunLinqTest(
            m => m.Where(x => (float)x.Year < 1990.5f),
            141,
            ["id-001", "id-002", "id-004", "id-005", "id-010"]
        );
    }

    [Fact]
    public async Task Linq_159()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.Equals("The Godfather")),
            1,
            ["id-001"]
        );
    }

    [Fact]
    public async Task Linq_160()
    {
        await RunLinqTest(
            m => m.Where(x => !x.Title.Equals("The Godfather")),
            247,
            ["id-000", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task Linq_161()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.Equals("The Godfather", StringComparison.Ordinal)),
            1,
            ["id-001"]
        );
    }

    [Fact]
    public async Task Linq_162()
    {
        await RunLinqTest(
            m => m.Where(x => !x.Title.Equals("The Godfather", StringComparison.Ordinal)),
            247,
            ["id-000", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task Linq_163()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.EndsWith("er", StringComparison.InvariantCulture)),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Linq_164()
    {
        await RunLinqTest(
            m => m.Where(x => x.Title.EndsWith("eR", StringComparison.InvariantCultureIgnoreCase)),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Linq_165()
    {
        DateTimeOffset dt = new(1994, 10, 14, 0, 0, 0, TimeSpan.Zero);
        await RunLinqTest(
            m => m.Where(x => x.UpdatedAt > dt).IncludeDeletedItems().OrderBy(x => x.Id).IncludeTotalCount().Skip(25),
            223,
            ["id-025", "id-026", "id-027", "id-028", "id-029"]
        );
    }

    [Fact]
    public async Task Linq_166()
    {
        await RunLinqTest(
            q => q.OrderBy(m => m.Id),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Linq_167()
    {
        await RunLinqTest(
            q => q.OrderBy(m => m.Id).Where(f => f.ReleaseDate.Month == 3),
            21,
            ["id-001", "id-017", "id-041", "id-050", "id-059", "id-066"]
        );
    }

    [Fact]
    public async Task Linq_168()
    {
        await RunLinqTest(
            q => q.OrderBy(m => m.Id).Where(f => f.ReleaseDate.Day == 21),
            9,
            ["id-010", "id-011", "id-021", "id-046", "id-085", "id-111"]
        );
    }

    [Fact]
    public async Task Linq_169()
    {
        await RunLinqTest(
            q => q.OrderBy(m => m.Id).Where(f => f.ReleaseDate.Year == 1990),
            2,
            ["id-014", "id-075"]
        );
    }

    [Fact]
    public async Task Linq_170()
    {
        await RunLinqTest(
            q => q.Where(m => m.Rating == MovieRating.PG).OrderBy(m => m.Id),
            40,
            ["id-010", "id-015", "id-024", "id-026", "id-027"]
        );
    }

    [Fact]
    public async Task Linq_171()
    {
        await RunLinqTest(
            q => q.Where(m => MovieRating.R == m.Rating).OrderBy(m => m.Id),
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Linq_172()
    {
        await RunLinqTest(
            q => q.Where(m => m.Rating != MovieRating.Unrated).OrderBy(m => m.Id),
            174,
            ["id-000", "id-001", "id-002", "id-003", "id-006", "id-007"]
        );
    }

    [Fact]
    public async Task Linq_173()
    {
        await RunLinqTest(
            q => q.Where(m => m.Title + "ouch" == "The Godfatherouch"),
            1,
            ["id-001"]
        );
    }
    #endregion

    #region Direct Linq Expressions
    [Fact]
    public async Task Enumerable_001()
    {
        await RunEnumerableTest(
            this.movieDataset.ToAsyncEnumerable(),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_002()
    {
        await RunEnumerableTest(
            this.movieDataset.IncludeTotalCount(),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_003()
    {
        await RunEnumerableTest(
            this.movieDataset.IncludeTotalCount(true),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_004()
    {
        await RunEnumerableTest(
            this.movieDataset.IncludeTotalCount(false),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_005()
    {
        await RunEnumerableTest(
            this.movieDataset.IncludeDeletedItems(),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_006()
    {
        await RunEnumerableTest(
            this.movieDataset.IncludeDeletedItems(true),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_007()
    {
        await RunEnumerableTest(
            this.movieDataset.IncludeDeletedItems(false),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_008()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_009()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Duration),
            Movies.MovieList.Length,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Enumerable_010()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Enumerable_011()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Title),
            Movies.MovieList.Length,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Enumerable_012()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Enumerable_013()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderByDescending(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_014()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderByDescending(x => x.Duration),
            Movies.MovieList.Length,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Enumerable_015()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderByDescending(x => x.Rating),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Enumerable_016()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Enumerable_017()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderByDescending(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Enumerable_018()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenBy(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_019()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenBy(x => x.Duration),
            Movies.MovieList.Length,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Enumerable_020()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Enumerable_021()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenBy(x => x.Title),
            Movies.MovieList.Length,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Enumerable_022()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenBy(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Enumerable_023()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenByDescending(x => x.BestPictureWinner),
            Movies.MovieList.Length,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_024()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenByDescending(x => x.Duration),
            Movies.MovieList.Length,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Enumerable_025()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenByDescending(x => x.Rating),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Enumerable_026()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Enumerable_027()
    {
        await RunEnumerableTest(
            this.movieDataset.ThenByDescending(x => x.ReleaseDate),
            Movies.MovieList.Length,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Enumerable_028()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Year).ThenBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Enumerable_029()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Year).ThenByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Enumerable_030()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderByDescending(x => x.Year).ThenBy(x => x.Rating),
            Movies.MovieList.Length,
            ["id-033", "id-122", "id-188", "id-102", "id-149"]
        );
    }

    [Fact]
    public async Task Enumerable_031()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderByDescending(x => x.Rating).ThenByDescending(x => x.Title),
            Movies.MovieList.Length,
            ["id-107", "id-160", "id-092", "id-176", "id-147"]
        );
    }

    [Fact]
    public async Task Enumerable_032()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.UpdatedAt),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_033()
    {
        await RunEnumerableTest(
            this.movieDataset.Skip(100),
            Movies.MovieList.Length - 100,
            ["id-100", "id-101", "id-102", "id-103", "id-104"]
        );
    }

    [Fact]
    public async Task Enumerable_034()
    {
        await RunEnumerableTest(
            this.movieDataset.Skip(200),
            Movies.MovieList.Length - 200,
            ["id-200", "id-201", "id-202", "id-203", "id-204"]
        );
    }

    [Fact]
    public async Task Enumerable_035()
    {
        await RunEnumerableTest(
            this.movieDataset.Take(100),
            100,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_036()
    {
        await RunEnumerableTest(
            this.movieDataset.Take(200),
            200,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_037()
    {
        await RunEnumerableTest(
            this.movieDataset.Skip(100).Take(50),
            50,
            ["id-100", "id-101", "id-102", "id-103", "id-104"]
        );
    }

    [Fact]
    public async Task Enumerable_038()
    {
        await RunEnumerableTest(
            this.movieDataset.Skip(200).Take(25),
            25,
            ["id-200", "id-201", "id-202", "id-203", "id-204"]
        );
    }

    [Fact]
    public async Task Enumerable_101()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.BestPictureWinner),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_102()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !x.BestPictureWinner),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_103()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.BestPictureWinner == true),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_104()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.BestPictureWinner == false),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_105()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.BestPictureWinner != true),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_106()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.BestPictureWinner != false),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_107()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.BestPictureWinner == true)),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_108()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.BestPictureWinner != true)),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_109()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.BestPictureWinner == false)),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_110()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.BestPictureWinner != false)),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_111()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Duration == 100),
            3,
            ["id-116", "id-159", "id-186"]
        );
    }

    [Fact]
    public async Task Enumerable_112()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Duration < 100),
            44,
            ["id-005", "id-037", "id-041", "id-044", "id-054"]
        );
    }

    [Fact]
    public async Task Enumerable_113()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Duration <= 100),
            47,
            ["id-005", "id-037", "id-041", "id-044", "id-054"]
        );
    }

    [Fact]
    public async Task Enumerable_114()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Duration > 90),
            227,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_115()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Duration >= 90),
            227,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_116()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Duration != 100),
            245,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_117()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.Duration == 100)),
            245,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_118()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.Duration < 100)),
            204,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_119()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.Duration <= 100)),
            201,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_120()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.Duration > 90)),
            21,
            ["id-041", "id-044", "id-054", "id-079", "id-089"]
        );
    }

    [Fact]
    public async Task Enumerable_121()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.Duration >= 90)),
            21,
            ["id-041", "id-044", "id-054", "id-079", "id-089"]
        );
    }

    [Fact]
    public async Task Enumerable_122()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !(x.Duration != 100)),
            3,
            ["id-116", "id-159", "id-186"]
        );
    }

    [Fact]
    public async Task Enumerable_123()
    {
        DateOnly dt = new(1994, 10, 14);
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate == dt),
            2,
            ["id-000", "id-003"]
        );
    }

    [Fact]
    public async Task Enumerable_124()
    {
        DateOnly dt = new(1999, 12, 31);
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate > dt),
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Enumerable_125()
    {
        DateOnly dt = new(1999, 12, 31);
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate >= dt),
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Enumerable_126()
    {
        DateOnly dt = new(2000, 1, 1);
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate < dt),
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_127()
    {
        DateOnly dt = new(2000, 1, 1);
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate <= dt),
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_128()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title == "The Godfather"),
            1,
            ["id-001"]
        );
    }

    [Fact]
    public async Task Enumerable_129()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title != "The Godfather"),
            247,
            ["id-000", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task Enumerable_130()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Year > 1929 && x.Year < 1940),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Enumerable_131()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Year >= 1930 && x.Year <= 1939),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Enumerable_132()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Year > 2000 || x.Year < 1940),
            78,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Enumerable_133()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Year > 2000 || !x.BestPictureWinner),
            218,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_134()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Year > 1929).Where(x => x.Year < 1940),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Enumerable_135()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Year >= 1930).Where(x => x.Year <= 1939),
            9,
            ["id-041", "id-044", "id-049", "id-106", "id-135"]
        );
    }

    [Fact]
    public async Task Enumerable_136()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Year >= 1930 && x.Year <= 1940) || (x.Year >= 1950 && x.Year <= 1960)),
            46,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [Fact]
    public async Task Enumerable_137()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Year - 1900) > 80),
            134,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [Fact]
    public async Task Enumerable_138()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Year + x.Duration) < 2100),
            103,
            ["id-005", "id-015", "id-016", "id-024", "id-026"]
        );
    }

    [Fact]
    public async Task Enumerable_139()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Year - 1900) < x.Duration),
            230,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_140()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Duration * 2) < 180),
            21,
            ["id-041", "id-044", "id-054", "id-079", "id-089"]
        );
    }

    [Fact]
    public async Task Enumerable_141()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Year / 1000.5) == 2),
            6,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [Fact]
    public async Task Enumerable_142()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Duration % 2) == 1),
            124,
            ["id-001", "id-004", "id-007", "id-008", "id-009"]
        );
    }

    [Fact]
    public async Task Enumerable_143()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (x.Year - 1900) >= 80 && (x.Year + 10) <= 2000 && x.Duration <= 120),
            13,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [Fact]
    public async Task Enumerable_144()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate.Day == 1),
            7,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [Fact]
    public async Task Enumerable_145()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate.Month == 11),
            14,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [Fact]
    public async Task Enumerable_146()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.ReleaseDate.Year != x.Year),
            52,
            ["id-004", "id-016", "id-024", "id-027", "id-029"]
        );
    }

    [Fact]
    public async Task Enumerable_147()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.EndsWith("er")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Enumerable_148()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.ToLower().EndsWith("er")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Enumerable_149()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.ToUpper().EndsWith("ER")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Enumerable_150()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.Trim().Length > 10),
            178,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_151()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => Math.Round(x.Duration / 60.0) == 2),
            186,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_152()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => Math.Ceiling(x.Duration / 60.0) == 2),
            124,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [Fact]
    public async Task Enumerable_153()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => Math.Floor(x.Duration / 60.0) == 2),
            120,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Enumerable_154()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !x.BestPictureWinner).Where(x => Math.Round(x.Duration / 60.0) == 2),
            162,
            ["id-000", "id-005", "id-009", "id-010", "id-013"]
        );
    }

    [Fact]
    public async Task Enumerable_155()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !x.BestPictureWinner).Where(x => Math.Ceiling(x.Duration / 60.0) == 2),
            113,
            ["id-005", "id-025", "id-026", "id-027", "id-028"]
        );
    }

    [Fact]
    public async Task Enumerable_156()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !x.BestPictureWinner).Where(x => Math.Floor(x.Duration / 60.0) == 2),
            99,
            ["id-000", "id-003", "id-004", "id-006", "id-009"]
        );
    }

    [Fact]
    public async Task Enumerable_157()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => Math.Round(x.Duration / 60.0) == (float)2),
            186,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Enumerable_158()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => (float)x.Year < 1990.5f),
            141,
            ["id-001", "id-002", "id-004", "id-005", "id-010"]
        );
    }

    [Fact]
    public async Task Enumerable_159()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.Equals("The Godfather")),
            1,
            ["id-001"]
        );
    }

    [Fact]
    public async Task Enumerable_160()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !x.Title.Equals("The Godfather")),
            247,
            ["id-000", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task Enumerable_161()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.Equals("The Godfather", StringComparison.Ordinal)),
            1,
            ["id-001"]
        );
    }

    [Fact]
    public async Task Enumerable_162()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => !x.Title.Equals("The Godfather", StringComparison.Ordinal)),
            247,
            ["id-000", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task Enumerable_163()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.EndsWith("er", StringComparison.InvariantCulture)),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Enumerable_164()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Title.EndsWith("eR", StringComparison.InvariantCultureIgnoreCase)),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Enumerable_165()
    {
        DateTimeOffset dt = new(1994, 10, 14, 0, 0, 0, TimeSpan.Zero);
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.UpdatedAt > dt).IncludeDeletedItems().OrderBy(x => x.Id).IncludeTotalCount().Skip(25),
            223,
            ["id-025", "id-026", "id-027", "id-028", "id-029"]
        );
    }

    [Fact]
    public async Task Enumerable_166()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Id),
            Movies.MovieList.Length,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Enumerable_167()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Id).Where(f => f.ReleaseDate.Month == 3),
            21,
            ["id-001", "id-017", "id-041", "id-050", "id-059", "id-066"]
        );
    }

    [Fact]
    public async Task Enumerable_168()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Id).Where(f => f.ReleaseDate.Day == 21),
            9,
            ["id-010", "id-011", "id-021", "id-046", "id-085", "id-111"]
        );
    }

    [Fact]
    public async Task Enumerable_169()
    {
        await RunEnumerableTest(
            this.movieDataset.OrderBy(x => x.Id).Where(f => f.ReleaseDate.Year == 1990),
            2,
            ["id-014", "id-075"]
        );
    }

    [Fact]
    public async Task Enumerable_170()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Rating == MovieRating.PG),
            40,
            ["id-010", "id-015", "id-024", "id-026", "id-027"]
        );
    }

    [Fact]
    public async Task Enumerable_171()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => MovieRating.R == x.Rating),
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Enumerable_172()
    {
        await RunEnumerableTest(
            this.movieDataset.Where(x => x.Rating != MovieRating.Unrated).OrderBy(x => x.Id),
            174,
            ["id-000", "id-001", "id-002", "id-003", "id-006", "id-007"]
        );
    }

    [Fact]
    public async Task Enumerable_173()
    {
        string[] expectedIds = ["id-000", "id-001", "id-002", "id-003", "id-004", "id-005"];
        List<T_IdAndTitle> list = await this.movieDataset.Select(x => new T_IdAndTitle() { Id = x.Id, Title = x.Title }).ToAsyncEnumerable().ToListAsync();
        list.Count.Should().Be(248);
        string[] actualIds = list.Take(expectedIds.Length).Select(m => m.Id).ToArray();
        actualIds.Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task Enumerable_174()
    {
        await RunEnumerableTest(
            this.movieDataset.WithParameter("X-Test", "foo").Where(x => x.Rating != MovieRating.Unrated).OrderBy(x => x.Id),
            174,
            ["id-000", "id-001", "id-002", "id-003", "id-006", "id-007"]
        );
    }
    #endregion
}
