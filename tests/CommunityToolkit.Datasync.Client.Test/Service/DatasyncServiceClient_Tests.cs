// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We want to test when devs do wierd things that the warnings protect against.
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Service;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using CommunityToolkit.Datasync.TestCommon.Models;
using FluentAssertions.Equivalency;
using FluentAssertions.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using HttpClientFactory = CommunityToolkit.Datasync.Client.Http.HttpClientFactory;

namespace CommunityToolkit.Datasync.Client.Test.Service;

[ExcludeFromCodeCoverage]
public class DatasyncServiceClient_Tests : IDisposable
{
    private readonly MockDelegatingHandler mockHandler = new();

    private readonly ClientKitchenSink successfulKitchenSink = new()
    {
        Id = Guid.NewGuid().ToString(),
        UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        Version = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
        Deleted = false,
        EnumValue = KitchenSinkState.Completed,
        StringValue = "test-string",
        DateOnlyValue = DateOnly.Parse("1997-05-04"),
        TimeOnlyValue = TimeOnly.Parse("12:34:56.789"),
        DateTimeValue = DateTime.Now,
        DateTimeOffsetValue = DateTimeOffset.UtcNow,
        GuidValue = Guid.NewGuid()
    };

    /// <summary>
    /// A test evaluator that ensures DateTimeOffset, DateTime, and TimeOnly values are msec resolution.
    /// </summary>
    private readonly Func<EquivalencyAssertionOptions<ClientKitchenSink>, EquivalencyAssertionOptions<ClientKitchenSink>> entityEquivalentOptions = (options) =>
    {
        options.Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Milliseconds())).WhenTypeIs<DateTimeOffset>();
        options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Milliseconds())).WhenTypeIs<DateTime>();
        options.Using<TimeOnly>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Milliseconds())).WhenTypeIs<TimeOnly>();
        return options;
    };

    #region AddAsync
    [Fact]
    public async Task AddAsync_Throws_On_Null()
    {
        ClientKitchenSink entity = null;
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.AddAsync(entity, options);
        await act.Should().ThrowAsync<ArgumentNullException>();

        entity = new();
        options = null;
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [MemberData(nameof(ClientTestData.InvalidIds), MemberType = typeof(ClientTestData))]
    public async Task AddAsync_Throws_InvalidId(string id)
    {
        ClientKitchenSink entity = new() { Id = id };
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.AddAsync(entity, options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddAsync_Success()
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, HttpStatusCode.Created));
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        string expected = JsonSerializer.Serialize(entity, DatasyncSerializer.JsonSerializerOptions);
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse<ClientKitchenSink> response = await client.AddAsync(entity, new DatasyncServiceOptions());

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.ToString().Should().Be("http://localhost/tables/kitchensink");
        (await request.Content.ReadAsStringAsync()).Should().Be(expected);

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(201);
        response.HasValue.Should().BeTrue();
        response.Value.Should().NotBeNull().And.BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task AddAsync_Conflict(HttpStatusCode code)
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, code));
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        string expected = """{"booleanValue":false,"intValue":0,"longValue":0,"decimalValue":0,"doubleValue":0,"floatValue":0,"nullableDouble":null,"charValue":"\u0000","stringValue":"abc","byteValue":0,"byteArrayValue":null,"enumValue":"None","nullableEnumValue":null,"guidValue":null,"dateTimeValue":"0001-01-01T08:00:00.000Z","dateTimeOffsetValue":"0001-01-01T00:00:00.000Z","dateOnlyValue":"0001-01-01","timeOnlyValue":"00:00:00.000","pointValue":null,"id":"1","updatedAt":null,"version":null,"deleted":false}""";
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.AddAsync(entity, new DatasyncServiceOptions());
        ConflictException<ClientKitchenSink> ex = (await act.Should().ThrowAsync<ConflictException<ClientKitchenSink>>()).Subject.First();

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.ToString().Should().Be("http://localhost/tables/kitchensink");
        (await request.Content.ReadAsStringAsync()).Should().Be(expected);

        ex.ClientEntity.Should().BeEquivalentTo(entity, this.entityEquivalentOptions);
        ex.ServerEntity.Should().BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.HasContent.Should().BeTrue();
        ex.ServiceResponse.IsConflictStatusCode.Should().BeTrue();
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.ReasonPhrase.Should().NotBeNullOrEmpty();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AddAsync_Error(HttpStatusCode code)
    {
        this.mockHandler.AddResponse(code);
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.AddAsync(entity, new DatasyncServiceOptions());
        DatasyncHttpException ex = (await act.Should().ThrowAsync<DatasyncHttpException>()).Subject.First();

        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_SuccessNoContent()
    {
        this.mockHandler.AddResponse(HttpStatusCode.OK);
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.AddAsync(entity, new DatasyncServiceOptions());
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task AddAsync_BadJson(HttpStatusCode code)
    {
        this.mockHandler.Responses.Add(GetBadJsonResponse(code));
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.AddAsync(entity, new DatasyncServiceOptions());
        await act.Should().ThrowAsync<JsonException>();
    }
    #endregion

    #region GetAsync
    [Fact]
    public async Task GetAsync_Throws_On_Null()
    {
        string id = null;
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.GetAsync(id, options);
        await act.Should().ThrowAsync<ArgumentNullException>();

        id = Guid.NewGuid().ToString("N");
        options = null;
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [MemberData(nameof(ClientTestData.InvalidIds), MemberType = typeof(ClientTestData))]
    public async Task GetAsync_Throws_InvalidId(string id)
    {
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.GetAsync(id, options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAsync_Success()
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, HttpStatusCode.OK));
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse<ClientKitchenSink> response = await client.GetAsync(id, new DatasyncServiceOptions());

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{id}");

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();
        response.Value.Should().NotBeNull().And.BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
    }

    [Fact]
    public async Task GetAsync_Success_IncludeDeleted()
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, HttpStatusCode.OK));
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        DatasyncServiceOptions options = new() { IncludeDeleted = true };
        ServiceResponse<ClientKitchenSink> response = await client.GetAsync(id, options);

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{id}?__includedeleted=true");

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();
        response.Value.Should().NotBeNull().And.BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
    }

    [Fact]
    public async Task GetAsync_SuccessNoContent()
    {
        this.mockHandler.AddResponse(HttpStatusCode.OK);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.GetAsync(id, new DatasyncServiceOptions());
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    public async Task GetAsync_Missing_Normal()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NotFound);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.GetAsync(id, new DatasyncServiceOptions());
        EntityDoesNotExistException ex = (await act.Should().ThrowAsync<EntityDoesNotExistException>()).Subject.Single();

        ex.Endpoint.Should().Be(client.Endpoint);
        ex.Id.Should().Be(id);
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAsync_Missing_DontThrow()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NotFound);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceOptions options = new() { ThrowIfMissing = false };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse<ClientKitchenSink> response = await client.GetAsync(id, options);

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{id}");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeFalse();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(404);
        response.HasValue.Should().BeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotModified)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task GetAsync_Error(HttpStatusCode code)
    {
        this.mockHandler.AddResponse(code);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.GetAsync(id, new DatasyncServiceOptions());
        DatasyncHttpException ex = (await act.Should().ThrowAsync<DatasyncHttpException>()).Subject.First();

        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
    }
    #endregion

    #region IncludeDeletedItems
    [Fact]
    public void Linq_IncludeDeletedItems_Default()
    {
        ExecuteQueryTest(
            x => x.IncludeDeletedItems(),
            "__includedeleted=true"
        );
    }

    [Fact]
    public void Linq_IncludeDeletedItems_True()
    {
        ExecuteQueryTest(
            x => x.IncludeDeletedItems(true),
            "__includedeleted=true"
        );
    }

    [Fact]
    public void Linq_IncludeDeletedItems_False()
    {
        ExecuteQueryTest(
            x => x.IncludeDeletedItems(false),
            ""
        );
    }
    #endregion

    #region IncludeTotalCount
    [Fact]
    public void Linq_IncludeTotalCount_Default()
    {
        ExecuteQueryTest(
            x => x.IncludeTotalCount(),
            "$count=true"
        );
    }

    [Fact]
    public void Linq_IncludeTotalCount_True()
    {
        ExecuteQueryTest(
            x => x.IncludeTotalCount(true),
            "$count=true"
        );
    }

    [Fact]
    public void Linq_IncludeTotalCount_False()
    {
        ExecuteQueryTest(
            x => x.IncludeTotalCount(false),
            ""
        );
    }
    #endregion

    #region OrderBy / OrderByDescending / ThenBy / ThenByDescending
    [Fact]
    public void Linq_OrderBy()
    {
        ExecuteQueryTest(
            x => x.OrderBy(m => m.StringValue),
            "$orderby=stringValue"
        );
    }

    [Fact]
    public void Linq_OrderByDescending()
    {
        ExecuteQueryTest(
            x => x.OrderByDescending(m => m.StringValue),
            "$orderby=stringValue desc"
        );
    }

    [Fact]
    public void Linq_ThenBy()
    {
        ExecuteQueryTest(
            x => x.ThenBy(m => m.StringValue),
            "$orderby=stringValue"
        );
    }

    [Fact]
    public void Linq_ThenByDescending()
    {
        ExecuteQueryTest(
            x => x.ThenByDescending(m => m.StringValue),
            "$orderby=stringValue desc"
        );
    }

    [Fact]
    public void Linq_OrderBy_ThenBy()
    {
        ExecuteQueryTest(
            x => x.OrderBy(m => m.StringValue).ThenBy(x => x.ByteValue),
            "$orderby=stringValue,byteValue"
        );
    }

    [Fact]
    public void Linq_OrderBy_ThenByDescending()
    {
        ExecuteQueryTest(
            x => x.OrderBy(m => m.StringValue).ThenByDescending(x => x.ByteValue),
            "$orderby=stringValue,byteValue desc"
        );
    }

    [Fact]
    public void Linq_OrderByDescending_ThenBy()
    {
        ExecuteQueryTest(
            x => x.OrderByDescending(m => m.StringValue).ThenBy(x => x.ByteValue),
            "$orderby=stringValue desc,byteValue"
        );
    }

    [Fact]
    public void Linq_OrderByDescending_ThenByDescending()
    {
        ExecuteQueryTest(
            x => x.OrderByDescending(m => m.StringValue).ThenByDescending(x => x.ByteValue),
            "$orderby=stringValue desc,byteValue desc"
        );
    }

    [Fact]
    public void Linq_OrderBy_InvalidMember()
    {
        ExecuteUnsupportedQueryTest<NotSupportedException>(
            x => x.OrderBy(m => m.IntValue % 7)
        );
    }

    [Fact]
    public void Linq_OrderByDescending_InvalidMember()
    {
        ExecuteUnsupportedQueryTest<NotSupportedException>(
            x => x.OrderByDescending(m => m.IntValue % 7)
        );
    }
    #endregion

    #region RemoveAsync
    [Fact]
    public async Task RemoveAsync_Throws_On_Null()
    {
        string id = null;
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.RemoveAsync(id, options);
        await act.Should().ThrowAsync<ArgumentNullException>();

        id = Guid.NewGuid().ToString("N");
        options = null;
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [MemberData(nameof(ClientTestData.InvalidIds), MemberType = typeof(ClientTestData))]
    public async Task RemoveAsync_Throws_InvalidId(string id)
    {
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.RemoveAsync(id, options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RemoveAsync_Success()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NoContent);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse response = await client.RemoveAsync(id, new DatasyncServiceOptions());

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{id}");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task RemoveAsync_SetsVersion()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NoContent);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceOptions options = new() { Version = "abcdefg1234" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse response = await client.RemoveAsync(id, options);

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{id}");
        request.Should().HaveHeader("If-Match", "\"abcdefg1234\"");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(204);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task RemoveAsync_Conflict(HttpStatusCode code)
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, code));
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.RemoveAsync(id, new DatasyncServiceOptions());
        ConflictException<ClientKitchenSink> ex = (await act.Should().ThrowAsync<ConflictException<ClientKitchenSink>>()).Subject.First();

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{id}");

        ex.ClientEntity.Should().BeNull();
        ex.ServerEntity.Should().BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.HasContent.Should().BeTrue();
        ex.ServiceResponse.IsConflictStatusCode.Should().BeTrue();
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.ReasonPhrase.Should().NotBeNullOrEmpty();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
    }

    [Fact]
    public async Task RemoveAsync_Missing_Normal()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NotFound);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.RemoveAsync(id, new DatasyncServiceOptions());
        EntityDoesNotExistException ex = (await act.Should().ThrowAsync<EntityDoesNotExistException>()).Subject.Single();

        ex.Endpoint.Should().Be(client.Endpoint);
        ex.Id.Should().Be(id);
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RemoveAsync_Missing_DontThrow()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NotFound);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceOptions options = new() { ThrowIfMissing = false };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse response = await client.RemoveAsync(id, options);

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{id}");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeFalse();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(404);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotModified)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task RemoveAsync_Error(HttpStatusCode code)
    {
        this.mockHandler.AddResponse(code);
        string id = Guid.NewGuid().ToString("N");
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.RemoveAsync(id, new DatasyncServiceOptions());
        DatasyncHttpException ex = (await act.Should().ThrowAsync<DatasyncHttpException>()).Subject.First();

        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
    }
    #endregion

    #region ReplaceAsync
    [Fact]
    public async Task ReplaceAsync_Throws_On_Null()
    {
        ClientKitchenSink entity = null;
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.ReplaceAsync(entity, options);
        await act.Should().ThrowAsync<ArgumentNullException>();

        entity = new() { Id = "1", StringValue = "abc" };
        options = null;
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [MemberData(nameof(ClientTestData.InvalidIds), MemberType = typeof(ClientTestData))]
    public async Task ReplaceAsync_Throws_InvalidId(string id)
    {
        ClientKitchenSink entity = new() { Id = id, StringValue = "abc" };
        DatasyncServiceOptions options = new();
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.ReplaceAsync(entity, options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReplaceAsync_Success()
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, HttpStatusCode.OK));
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        string expected = JsonSerializer.Serialize(entity, DatasyncSerializer.JsonSerializerOptions);
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse<ClientKitchenSink> response = await client.ReplaceAsync(entity, new DatasyncServiceOptions());

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{entity.Id}");
        (await request.Content.ReadAsStringAsync()).Should().Be(expected);

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();
        response.Value.Should().NotBeNull().And.BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
    }

    [Fact]
    public async Task ReplaceAsync_SetsVersion()
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, HttpStatusCode.OK));
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        string expected = JsonSerializer.Serialize(entity, DatasyncSerializer.JsonSerializerOptions);
        DatasyncServiceOptions options = new() { Version = "abcdefg1234" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse<ClientKitchenSink> response = await client.ReplaceAsync(entity, options);

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{entity.Id}");
        request.Should().HaveHeader("If-Match", "\"abcdefg1234\"");
        (await request.Content.ReadAsStringAsync()).Should().Be(expected);

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();
        response.Value.Should().NotBeNull().And.BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task ReplaceAsync_Conflict(HttpStatusCode code)
    {
        this.mockHandler.Responses.Add(GetSuccessfulResponse(this.successfulKitchenSink, code));
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        string expected = JsonSerializer.Serialize(entity, DatasyncSerializer.JsonSerializerOptions);
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.ReplaceAsync(entity, new DatasyncServiceOptions());
        ConflictException<ClientKitchenSink> ex = (await act.Should().ThrowAsync<ConflictException<ClientKitchenSink>>()).Subject.First();

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{entity.Id}");
        (await request.Content.ReadAsStringAsync()).Should().Be(expected);

        ex.ClientEntity.Should().BeEquivalentTo(entity, this.entityEquivalentOptions);
        ex.ServerEntity.Should().BeEquivalentTo(this.successfulKitchenSink, this.entityEquivalentOptions);
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.HasContent.Should().BeTrue();
        ex.ServiceResponse.IsConflictStatusCode.Should().BeTrue();
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.ReasonPhrase.Should().NotBeNullOrEmpty();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
    }

    [Fact]
    public async Task ReplaceAsync_SuccessNoContent()
    {
        this.mockHandler.AddResponse(HttpStatusCode.OK);
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.ReplaceAsync(entity, new DatasyncServiceOptions());
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    public async Task ReplaceAsync_Missing_Normal()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NotFound);
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();

        Func<Task> act = async () => _ = await client.ReplaceAsync(entity, new DatasyncServiceOptions());
        EntityDoesNotExistException ex = (await act.Should().ThrowAsync<EntityDoesNotExistException>()).Subject.Single();

        ex.Endpoint.Should().Be(client.Endpoint);
        ex.Id.Should().Be(entity.Id);
        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ReplaceAsync_Missing_DontThrow()
    {
        this.mockHandler.AddResponse(HttpStatusCode.NotFound);
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        DatasyncServiceOptions options = new() { ThrowIfMissing = false };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        ServiceResponse response = await client.ReplaceAsync(entity, options);

        HttpRequestMessage request = this.mockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"http://localhost/tables/kitchensink/{entity.Id}");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeFalse();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(404);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotModified)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReplaceAsync_Error(HttpStatusCode code)
    {
        this.mockHandler.AddResponse(code);
        ClientKitchenSink entity = new() { Id = "1", StringValue = "abc" };
        DatasyncServiceClient<ClientKitchenSink> client = GetMockClient<ClientKitchenSink>();
        Func<Task> act = async () => _ = await client.ReplaceAsync(entity, new DatasyncServiceOptions());
        DatasyncHttpException ex = (await act.Should().ThrowAsync<DatasyncHttpException>()).Subject.First();

        ex.ServiceResponse.Should().NotBeNull();
        ex.ServiceResponse.StatusCode.Should().Be((int)code);
        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
    }
    #endregion

    #region Select
    [Fact]
    public void Linq_Select_Anonymous()
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        string query = client.Select(m => new { m.Id, m.StringValue }).ToODataQueryString();
        string actual = Uri.UnescapeDataString(query);
        actual.Should().Be("$select=id,stringValue");
    }

    [Fact]
    public void Linq_Select_Anonymous_NullNamingPolicy()
    {
        JsonSerializerOptions serializerOptions = new();
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        string query = client.Select(m => new { m.Id, m.StringValue }).ToODataQueryString();
        string actual = Uri.UnescapeDataString(query);
        actual.Should().Be("$select=Id,StringValue");
    }

    [Fact]
    public void Linq_Select_Named()
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        string query = client.Select(m => new NamedSelectClass() { Id = m.Id, StringValue = m.StringValue }).ToODataQueryString();
        string actual = Uri.UnescapeDataString(query);
        actual.Should().Be("$select=id,stringValue");
    }

    // TODO: Required Additional Parameters
    #endregion

    #region Skip
    [Fact]
    public void Linq_Skip_0()
    {
        ExecuteQueryTest(
            x => x.Skip(0),
            ""
        );
    }

    [Fact]
    public void Linq_Skip_1()
    {
        ExecuteQueryTest(
            x => x.Skip(1),
            "$skip=1"
        );
    }

    [Fact]
    public void Linq_Skip_42()
    {
        ExecuteQueryTest(
            x => x.Skip(42),
            "$skip=42"
        );
    }

    [Fact]
    public void Linq_Skip_40_Then_2()
    {
        ExecuteQueryTest(
            x => x.Skip(40).Skip(2),
            "$skip=42"
        );
    }
    #endregion

    #region Top
    [Fact]
    public void Linq_Take_0()
    {
        ExecuteQueryTest(
            x => x.Take(0),
            "$top=0"
        );
    }

    [Fact]
    public void Linq_Take_42()
    {
        ExecuteQueryTest(
            x => x.Take(42),
            "$top=42"
        );
    }

    [Fact]
    public void Linq_Take_40_Then_2()
    {
        ExecuteQueryTest(
            x => x.Take(40).Take(2),
            "$top=2"
        );
    }

    [Fact]
    public void Linq_Take_2_Then_40()
    {
        ExecuteQueryTest(
            x => x.Take(2).Take(40),
            "$top=2"
        );
    }
    #endregion

    #region Where
    #region Boolean Comparisons
    [Fact]
    public void Linq_Where_Boolean_NotBoolean()
    {
        ExecuteQueryTest(
            x => x.Where(m => !m.BooleanValue),
            "$filter=not(booleanValue)"
        );
    }

    [Fact]
    public void Linq_Where_Boolean_IsFalse()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.BooleanValue == false),
            "$filter=(booleanValue eq false)"
        );
    }

    [Fact]
    public void Linq_Where_Boolean_IsBoolean()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.BooleanValue),
            "$filter=booleanValue");
    }

    [Fact]
    public void Linq_Where_Boolean_IsTrue()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.BooleanValue == true),
            "$filter=(booleanValue eq true)"
        );
    }

    [Fact]
    public void Linq_Where_Boolean_IsNotFalse()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.BooleanValue != false),
            "$filter=(booleanValue ne false)");
    }

    [Fact]
    public void Linq_Where_Boolean_IsNotTrue()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.BooleanValue != true),
            "$filter=(booleanValue ne true)");
    }
    #endregion

    #region Byte Comparisons
    [Fact]
    public void Linq_Where_Byte_Equal()
    {
        byte comparison = (byte)42;
        ExecuteQueryTest(
            x => x.Where(m => m.ByteValue == comparison),
            "$filter=(byteValue eq 42)"
        );
    }

    [Fact]
    public void Linq_Where_Byte_NotEqual()
    {
        byte comparison = (byte)42;
        ExecuteQueryTest(
            x => x.Where(m => m.ByteValue != comparison),
            "$filter=(byteValue ne 42)"
        );
    }

    [Fact]
    public void Linq_Where_Byte_GreaterThen()
    {
        byte comparison = (byte)42;
        ExecuteQueryTest(
            x => x.Where(m => m.ByteValue > comparison),
            "$filter=(byteValue gt 42)"
        );
    }

    [Fact]
    public void Linq_Where_Byte_GreaterThanOrEqual()
    {
        byte comparison = (byte)42;
        ExecuteQueryTest(
            x => x.Where(m => m.ByteValue >= comparison),
            "$filter=(byteValue ge 42)"
        );
    }

    [Fact]
    public void Linq_Where_Byte_LessThan()
    {
        byte comparison = (byte)42;
        ExecuteQueryTest(
            x => x.Where(m => m.ByteValue < comparison),
            "$filter=(byteValue lt 42)"
        );
    }

    [Fact]
    public void Linq_Where_Byte_LessThanOrEqual()
    {
        byte comparison = (byte)42;
        ExecuteQueryTest(
            x => x.Where(m => m.ByteValue <= comparison),
            "$filter=(byteValue le 42)"
        );
    }
    #endregion

    #region DateOnly Comparisons
    [Fact]
    public void Linq_Where_DateOnly_Equal()
    {
        DateOnly comparison = new(1977, 5, 4);
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue == comparison),
            "$filter=(dateOnlyValue eq cast(1977-05-04,Edm.Date))"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_NotEqual()
    {
        DateOnly comparison = new(1977, 5, 4);
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue == comparison),
            "$filter=(dateOnlyValue eq cast(1977-05-04,Edm.Date))"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_GreaterThan()
    {
        DateOnly comparison = new(1977, 5, 4);
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue > comparison),
            "$filter=(dateOnlyValue gt cast(1977-05-04,Edm.Date))"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_GreaterThanOrEqual()
    {
        DateOnly comparison = new(1977, 5, 4);
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue >= comparison),
            "$filter=(dateOnlyValue ge cast(1977-05-04,Edm.Date))"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_LessThan()
    {
        DateOnly comparison = new(1977, 5, 4);
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue < comparison),
            "$filter=(dateOnlyValue lt cast(1977-05-04,Edm.Date))"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_LessThanOrEqual()
    {
        DateOnly comparison = new(1977, 5, 4);
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue <= comparison),
            "$filter=(dateOnlyValue le cast(1977-05-04,Edm.Date))"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_DayEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Day == 4),
            "$filter=(day(dateOnlyValue) eq 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_DayNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Day != 4),
            "$filter=(day(dateOnlyValue) ne 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_DayGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Day > 4),
            "$filter=(day(dateOnlyValue) gt 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_DayLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Day < 4),
            "$filter=(day(dateOnlyValue) lt 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_MonthEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Month == 5),
            "$filter=(month(dateOnlyValue) eq 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_MonthNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Month != 5),
            "$filter=(month(dateOnlyValue) ne 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_MonthGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Month > 5),
            "$filter=(month(dateOnlyValue) gt 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_MonthLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Month < 5),
            "$filter=(month(dateOnlyValue) lt 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_YearEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Year == 1977),
            "$filter=(year(dateOnlyValue) eq 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_YearNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Year != 1977),
            "$filter=(year(dateOnlyValue) ne 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_YearGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Year > 1977),
            "$filter=(year(dateOnlyValue) gt 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateOnly_YearLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateOnlyValue.Year < 1977),
            "$filter=(year(dateOnlyValue) lt 1977)"
        );
    }
    #endregion

    #region DateTime Comparisons
    [Fact]
    public void Linq_Where_DateTime_Equal()
    {
        DateTime comparison = new(1977, 5, 4, 18, 30, 45, 107, DateTimeKind.Utc);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue == comparison),
            "$filter=(dateTimeValue eq cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_NotEqual()
    {
        DateTime comparison = new(1977, 5, 4, 18, 30, 45, 107, DateTimeKind.Utc);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue == comparison),
            "$filter=(dateTimeValue eq cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_GreaterThan()
    {
        DateTime comparison = new(1977, 5, 4, 18, 30, 45, 107, DateTimeKind.Utc);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue > comparison),
            "$filter=(dateTimeValue gt cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_GreaterThanOrEqual()
    {
        DateTime comparison = new(1977, 5, 4, 18, 30, 45, 107, DateTimeKind.Utc);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue >= comparison),
            "$filter=(dateTimeValue ge cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_LessThan()
    {
        DateTime comparison = new(1977, 5, 4, 18, 30, 45, 107, DateTimeKind.Utc);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue < comparison),
            "$filter=(dateTimeValue lt cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_LessThanOrEqual()
    {
        DateTime comparison = new(1977, 5, 4, 18, 30, 45, 107, DateTimeKind.Utc);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue <= comparison),
            "$filter=(dateTimeValue le cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_DayEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Day == 4),
            "$filter=(day(dateTimeValue) eq 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_DayNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Day != 4),
            "$filter=(day(dateTimeValue) ne 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_DayGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Day > 4),
            "$filter=(day(dateTimeValue) gt 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_DayLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Day < 4),
            "$filter=(day(dateTimeValue) lt 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MonthEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Month == 5),
            "$filter=(month(dateTimeValue) eq 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MonthNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Month != 5),
            "$filter=(month(dateTimeValue) ne 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MonthGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Month > 5),
            "$filter=(month(dateTimeValue) gt 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MonthLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Month < 5),
            "$filter=(month(dateTimeValue) lt 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_YearEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Year == 1977),
            "$filter=(year(dateTimeValue) eq 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_YearNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Year != 1977),
            "$filter=(year(dateTimeValue) ne 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_YearGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Year > 1977),
            "$filter=(year(dateTimeValue) gt 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_YearLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Year < 1977),
            "$filter=(year(dateTimeValue) lt 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_HourEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Hour == 12),
            "$filter=(hour(dateTimeValue) eq 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_HourNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Hour != 12),
            "$filter=(hour(dateTimeValue) ne 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_HourGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Hour > 12),
            "$filter=(hour(dateTimeValue) gt 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_HourLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Hour < 12),
            "$filter=(hour(dateTimeValue) lt 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MinuteEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Minute == 30),
            "$filter=(minute(dateTimeValue) eq 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MinuteNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Minute != 30),
            "$filter=(minute(dateTimeValue) ne 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MinuteGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Minute > 30),
            "$filter=(minute(dateTimeValue) gt 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_MinuteLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Minute < 30),
            "$filter=(minute(dateTimeValue) lt 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_SecondEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Second == 30),
            "$filter=(second(dateTimeValue) eq 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_SecondNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Second != 30),
            "$filter=(second(dateTimeValue) ne 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_SecondGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Second > 30),
            "$filter=(second(dateTimeValue) gt 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTime_SecondLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeValue.Second < 30),
            "$filter=(second(dateTimeValue) lt 30)"
        );
    }
    #endregion

    #region DateTimeOffset Comparisons
    [Fact]
    public void Linq_Where_DateTimeOffset_Equal()
    {
        DateTimeOffset comparison = new(1977, 5, 4, 18, 30, 45, 107, TimeSpan.Zero);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue == comparison),
            "$filter=(dateTimeOffsetValue eq cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_NotEqual()
    {
        DateTimeOffset comparison = new(1977, 5, 4, 18, 30, 45, 107, TimeSpan.Zero);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue == comparison),
            "$filter=(dateTimeOffsetValue eq cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_GreaterThan()
    {
        DateTimeOffset comparison = new(1977, 5, 4, 18, 30, 45, 107, TimeSpan.Zero);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue > comparison),
            "$filter=(dateTimeOffsetValue gt cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_GreaterThanOrEqual()
    {
        DateTimeOffset comparison = new(1977, 5, 4, 18, 30, 45, 107, TimeSpan.Zero);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue >= comparison),
            "$filter=(dateTimeOffsetValue ge cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_LessThan()
    {
        DateTimeOffset comparison = new(1977, 5, 4, 18, 30, 45, 107, TimeSpan.Zero);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue < comparison),
            "$filter=(dateTimeOffsetValue lt cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_LessThanOrEqual()
    {
        DateTimeOffset comparison = new(1977, 5, 4, 18, 30, 45, 107, TimeSpan.Zero);
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue <= comparison),
            "$filter=(dateTimeOffsetValue le cast(1977-05-04T18:30:45.107Z,Edm.DateTimeOffset))"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_DayEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Day == 4),
            "$filter=(day(dateTimeOffsetValue) eq 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_DayNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Day != 4),
            "$filter=(day(dateTimeOffsetValue) ne 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_DayGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Day > 4),
            "$filter=(day(dateTimeOffsetValue) gt 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_DayLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Day < 4),
            "$filter=(day(dateTimeOffsetValue) lt 4)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MonthEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Month == 5),
            "$filter=(month(dateTimeOffsetValue) eq 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MonthNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Month != 5),
            "$filter=(month(dateTimeOffsetValue) ne 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MonthGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Month > 5),
            "$filter=(month(dateTimeOffsetValue) gt 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MonthLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Month < 5),
            "$filter=(month(dateTimeOffsetValue) lt 5)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_YearEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Year == 1977),
            "$filter=(year(dateTimeOffsetValue) eq 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_YearNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Year != 1977),
            "$filter=(year(dateTimeOffsetValue) ne 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_YearGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Year > 1977),
            "$filter=(year(dateTimeOffsetValue) gt 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_YearLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Year < 1977),
            "$filter=(year(dateTimeOffsetValue) lt 1977)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_HourEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Hour == 12),
            "$filter=(hour(dateTimeOffsetValue) eq 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_HourNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Hour != 12),
            "$filter=(hour(dateTimeOffsetValue) ne 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_HourGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Hour > 12),
            "$filter=(hour(dateTimeOffsetValue) gt 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_HourLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Hour < 12),
            "$filter=(hour(dateTimeOffsetValue) lt 12)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MinuteEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Minute == 30),
            "$filter=(minute(dateTimeOffsetValue) eq 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MinuteNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Minute != 30),
            "$filter=(minute(dateTimeOffsetValue) ne 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MinuteGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Minute > 30),
            "$filter=(minute(dateTimeOffsetValue) gt 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_MinuteLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Minute < 30),
            "$filter=(minute(dateTimeOffsetValue) lt 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_SecondEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Second == 30),
            "$filter=(second(dateTimeOffsetValue) eq 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_SecondNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Second != 30),
            "$filter=(second(dateTimeOffsetValue) ne 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_SecondGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Second > 30),
            "$filter=(second(dateTimeOffsetValue) gt 30)"
        );
    }

    [Fact]
    public void Linq_Where_DateTimeOffset_SecondLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.DateTimeOffsetValue.Second < 30),
            "$filter=(second(dateTimeOffsetValue) lt 30)"
        );
    }
    #endregion

    #region Decimal Comparisons
    [Fact]
    public void Linq_Where_DecimalCeiling()
    {
        ExecuteQueryTest(
            x => x.Where(m => Math.Ceiling(m.DecimalValue) == 2.0M),
            "$filter=(ceiling(decimalValue) eq 2.0M)"
        );
    }

    [Fact]
    public void Linq_Where_DecimalFloor()
    {
        ExecuteQueryTest(
            x => x.Where(m => Math.Floor(m.DecimalValue) == 42M),
            "$filter=(floor(decimalValue) eq 42M)"
        );
    }

    [Fact]
    public void Linq_Where_DecimalRound()
    {
        ExecuteQueryTest(
            x => x.Where(m => Math.Round(m.DecimalValue) == 42M),
            "$filter=(round(decimalValue) eq 42M)"
        );
    }
    #endregion

    #region Double Comparisons
    [Fact]
    public void Linq_Where_DoubleCeiling()
    {
        ExecuteQueryTest(
            x => x.Where(m => Math.Ceiling(m.DoubleValue) == 2),
            "$filter=(ceiling(doubleValue) eq 2.0)"
        );
    }

    [Fact]
    public void Linq_Where_DoubleFloor()
    {
        ExecuteQueryTest(
            x => x.Where(m => Math.Floor(m.DoubleValue) == 42),
            "$filter=(floor(doubleValue) eq 42.0)"
        );
    }

    [Fact]
    public void Linq_Where_DoubleRound()
    {
        ExecuteQueryTest(
            x => x.Where(m => Math.Round(m.DoubleValue) == 42),
            "$filter=(round(doubleValue) eq 42.0)"
        );
    }
    #endregion

    #region Enum Comparisons
    [Fact]
    public void Linq_Where_Enum_Equal()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.EnumValue == KitchenSinkState.Completed),
            "$filter=(enumValue eq 'Completed')"
        );
    }

    [Fact]
    public void Linq_Where_Enum_NotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.EnumValue != KitchenSinkState.Completed),
            "$filter=(enumValue ne 'Completed')"
        );
    }

    [Fact]
    public void Linq_Where_Enum_Equal_Reversed()
    {
        ExecuteQueryTest(
            x => x.Where(m => KitchenSinkState.Completed == m.EnumValue),
            "$filter=(enumValue eq 'Completed')"
        );
    }
    #endregion

    #region Guid Comparisons
    [Fact]
    public void Linq_Where_Guid_Equal()
    {
        Guid comparison = Guid.NewGuid();
        ExecuteQueryTest(
            x => x.Where(m => m.GuidValue == comparison),
            $"$filter=(guidValue eq cast({comparison:D},Edm.Guid))"
        );
    }

    [Fact]
    public void Linq_Where_Guid_NotEqual()
    {
        Guid comparison = Guid.NewGuid();
        ExecuteQueryTest(
            x => x.Where(m => m.GuidValue != comparison),
            $"$filter=(guidValue ne cast({comparison:D},Edm.Guid))"
        );
    }
    #endregion

    #region Int Comparisons
    [Fact]
    public void Linq_Where_Int_Equal()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue == 42),
            "$filter=(intValue eq 42)"
        );
    }

    [Fact]
    public void Linq_Where_Int_NotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue != 42),
            "$filter=(intValue ne 42)"
        );
    }

    [Fact]
    public void Linq_Where_Int_GreaterThen()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue > 42),
            "$filter=(intValue gt 42)"
        );
    }

    [Fact]
    public void Linq_Where_Int_GreaterThanOrEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue >= 42),
            "$filter=(intValue ge 42)"
        );
    }

    [Fact]
    public void Linq_Where_Int_LessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue < 42),
            "$filter=(intValue lt 42)"
        );
    }

    [Fact]
    public void Linq_Where_Int_LessThanOrEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue <= 42),
            "$filter=(intValue le 42)"
        );
    }
    #endregion

    #region String Comparisons
    [Fact]
    public void Linq_Where_StringConcat()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue + "er" == "lager"),
            "$filter=(concat(stringValue,'er') eq 'lager')"
        );
    }

    [Fact]
    public void Linq_Where_Equals_null()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue == null),
            "$filter=(stringValue eq null)"
        );
    }

    [Fact]
    public void Linq_Where_EndsWith_NoStringComparison()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.EndsWith("abc")),
            "$filter=endswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_Where_EndsWith_Ordinal()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.EndsWith("abc", StringComparison.Ordinal)),
            "$filter=endswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_Where_EndsWith_Invariant()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.EndsWith("abc", StringComparison.InvariantCulture)),
            "$filter=endswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_Where_EndsWith_OrdinalIgnoreCase()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.EndsWith("abc", StringComparison.OrdinalIgnoreCase)),
            "$filter=endswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Where_EndsWith_InvariantIgnoreCase()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.EndsWith("abc", StringComparison.InvariantCultureIgnoreCase)),
            "$filter=endswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Where_Equals_NoStringComparison()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.Equals("abc")),
            "$filter=(stringValue eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Where_Equals_Ordinal()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.Equals("abc", StringComparison.Ordinal)),
            "$filter=(stringValue eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Where_Equals_Invariant()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.Equals("abc", StringComparison.InvariantCulture)),
            "$filter=(stringValue eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Where_Equals_OrdinalIgnoreCase()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.Equals("abc", StringComparison.OrdinalIgnoreCase)),
            "$filter=(tolower(stringValue) eq tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Where_Equals_InvariantIgnoreCase()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.Equals("abc", StringComparison.InvariantCultureIgnoreCase)),
            "$filter=(tolower(stringValue) eq tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Where_StartsWith_NoStringComparison()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.StartsWith("abc")),
            "$filter=startswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_Where_StartsWith_Ordinal()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.StartsWith("abc", StringComparison.Ordinal)),
            "$filter=startswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_Where_StartsWith_Invariant()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.StartsWith("abc", StringComparison.InvariantCulture)),
            "$filter=startswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_Where_StartsWith_OrdinalIgnoreCase()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.StartsWith("abc", StringComparison.OrdinalIgnoreCase)),
            "$filter=startswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Where_StartsWith_InvariantIgnoreCase()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.StartsWith("abc", StringComparison.InvariantCultureIgnoreCase)),
            "$filter=startswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Where_String_Contains()
    {
        string[] ratings = ["A", "B"];
        ExecuteQueryTest(
            x => x.Where(m => ratings.Contains(m.StringValue)),
            "$filter=stringValue in ('A','B')"
        );
    }

    [Fact]
    public void Linq_Where_EndsWith_ToUpper()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue.ToUpperInvariant() == "ER"),
            "$filter=(toupper(stringValue) eq 'ER')"
        );
    }

    [Fact]
    public void Linq_Where_StringEquals()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.StringValue == "test"),
            "$filter=(stringValue eq 'test')"
        );
    }
    #endregion

    #region TimeOnly Comparisons
    [Fact]
    public void Linq_Where_TimeOnly_Equal()
    {
        TimeOnly comparison = new(18, 33, 45, 876);
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue == comparison),
            "$filter=(timeOnlyValue eq cast(18:33:45.876,Edm.TimeOfDay))"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_NotEqual()
    {
        TimeOnly comparison = new(18, 33, 45, 876);
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue == comparison),
            "$filter=(timeOnlyValue eq cast(18:33:45.876,Edm.TimeOfDay))"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_GreaterThan()
    {
        TimeOnly comparison = new(18, 33, 45, 876);
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue > comparison),
            "$filter=(timeOnlyValue gt cast(18:33:45.876,Edm.TimeOfDay))"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_GreaterThanOrEqual()
    {
        TimeOnly comparison = new(18, 33, 45, 876);
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue >= comparison),
            "$filter=(timeOnlyValue ge cast(18:33:45.876,Edm.TimeOfDay))"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_LessThan()
    {
        TimeOnly comparison = new(18, 33, 45, 876);
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue < comparison),
            "$filter=(timeOnlyValue lt cast(18:33:45.876,Edm.TimeOfDay))"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_LessThanOrEqual()
    {
        TimeOnly comparison = new(18, 33, 45, 876);
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue <= comparison),
            "$filter=(timeOnlyValue le cast(18:33:45.876,Edm.TimeOfDay))"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_HourEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Hour == 12),
            "$filter=(hour(timeOnlyValue) eq 12)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_HourNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Hour != 12),
            "$filter=(hour(timeOnlyValue) ne 12)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_HourGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Hour > 12),
            "$filter=(hour(timeOnlyValue) gt 12)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_HourLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Hour < 12),
            "$filter=(hour(timeOnlyValue) lt 12)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_MinuteEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Minute == 30),
            "$filter=(minute(timeOnlyValue) eq 30)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_MinuteNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Minute != 30),
            "$filter=(minute(timeOnlyValue) ne 30)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_MinuteGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Minute > 30),
            "$filter=(minute(timeOnlyValue) gt 30)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_MinuteLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Minute < 30),
            "$filter=(minute(timeOnlyValue) lt 30)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_SecondEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Second == 30),
            "$filter=(second(timeOnlyValue) eq 30)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_SecondNotEqual()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Second != 30),
            "$filter=(second(timeOnlyValue) ne 30)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_SecondGreaterThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Second > 30),
            "$filter=(second(timeOnlyValue) gt 30)"
        );
    }

    [Fact]
    public void Linq_Where_TimeOnly_SecondLessThan()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Second < 30),
            "$filter=(second(timeOnlyValue) lt 30)"
        );
    }
    #endregion

    #region Math Comparisons
    [Fact]
    public void Linq_Where_Addition()
    {
        ExecuteQueryTest(
            x => x.Where(m => (m.IntValue + 2) == 42),
            "$filter=((intValue add 2) eq 42)"
        );
    }

    [Fact]
    public void Linq_Where_Division()
    {
        ExecuteQueryTest(
            x => x.Where(m => (m.IntValue / 60.0) == 2),
            "$filter=((intValue div 60.0) eq 2.0)"
        );
    }

    [Fact]
    public void Linq_Where_Modulo()
    {
        ExecuteQueryTest(
            x => x.Where(m => (m.IntValue % 60.0) == 2),
            "$filter=((intValue mod 60.0) eq 2.0)"
        );
    }

    [Fact]
    public void Linq_Where_Multiplication()
    {
        ExecuteQueryTest(
            x => x.Where(m => (m.IntValue * 12.5) == 2),
            "$filter=((intValue mul 12.5) eq 2.0)"
        );
    }

    [Fact]
    public void Linq_Where_Subtract()
    {
        ExecuteQueryTest(
            x => x.Where(m => (m.IntValue - 1900) == 42),
            "$filter=((intValue sub 1900) eq 42)"
        );
    }
    #endregion

    #region Composite Comparisons
    [Fact]
    public void Linq_Where_Composite_And()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue > 1900 && m.IntValue < 2000),
            "$filter=((intValue gt 1900) and (intValue lt 2000))"
        );
    }

    [Fact]
    public void Linq_Where_Composite_Or()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue > 1900 || m.BooleanValue),
            "$filter=((intValue gt 1900) or booleanValue)"
        );
    }

    [Fact]
    public void Linq_Multiple_Where()
    {
        ExecuteQueryTest(
            x => x.Where(m => m.IntValue > 1900).Where(m => m.IntValue < 2000),
            "$filter=((intValue gt 1900) and (intValue lt 2000))"
        );
    }
    #endregion

    #region Unsupported Operations
    [Fact]
    public void Linq_Where_Call_Unsupported()
    {
        ExecuteUnsupportedQueryTest<NotSupportedException>(
            x => x.Where(m => m.ByteArrayValue.LongCount() > 0)
        );
    }

    [Fact]
    public void Linq_Where_InvalidMathMethod_Unsupported()
    {
        ExecuteUnsupportedQueryTest<NotSupportedException>(
            x => x.Where(m => Math.Sqrt(m.DoubleValue) == 4.0)
        );
    }

    [Fact]
    public void Linq_Where_InvalidStringComparison_Unsupported()
    {
        ExecuteUnsupportedQueryTest<NotSupportedException>(
            x => x.Where(m => m.StringValue.Equals("er", StringComparison.CurrentCultureIgnoreCase))
        );
    }

    [Fact]
    public void Linq_Where_VariableStringComparison_Unsupported()
    {
        ExecuteUnsupportedQueryTest<NotSupportedException>(
            x => x.Where(m => m.StringValue.Equals("er", m.BooleanValue ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        );
    }

    [Fact]
    public void Linq_Where_TimeProperties_Unsupported()
    {
        ExecuteUnsupportedQueryTest<NotSupportedException>(
            x => x.Where(m => m.DateTimeOffsetValue.Microsecond > 0)
        );
    }
    #endregion
    #endregion

    #region WithParameter
    [Fact]
    public void Linq_WithParameter_SetsParameter()
    {
        ExecuteQueryTest(
            x => x.WithParameter("foo", "bar"),
            "foo=bar"
        );
    }

    [Fact]
    public void Linq_WithParameter_OverwritesParameter()
    {
        ExecuteQueryTest(
            x => x.WithParameter("foo", "bar").WithParameter("foo", "baz"),
            "foo=baz"
        );
    }

    [Fact]
    public void Linq_WithParameter_MultiParameter()
    {
        ExecuteQueryTest(
            x => x.WithParameter("foo", "bar").WithParameter("bar", "baz"),
            "bar=baz&foo=bar"
        );
    }

    [Fact]
    public void Linq_WithParameter_Exceptions()
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        Action act1 = () => _ = client.AsQueryable().WithParameter("$foo", "bar");
        act1.Should().Throw<ArgumentException>();

        Action act2 = () => _ = client.AsQueryable().WithParameter("__foo", "bar");
        act2.Should().Throw<ArgumentException>();
    }
    #endregion

    #region WithParameters
    [Fact]
    public void Linq_WithParameters_SetsAllParameters()
    {
        Dictionary<string, string> dict = new()
        {
            { "foo", "bar" },
            { "bar", "baz" }
        };

        ExecuteQueryTest(
            x => x.WithParameters(dict),
            "bar=baz&foo=bar"
        );
    }

    [Fact]
    public void Linq_WithParameters_OverwritesParameters()
    {
        Dictionary<string, string> dict = new()
        {
            { "foo", "bar" },
            { "bar", "baz" }
        };

        ExecuteQueryTest(
            x => x.WithParameter("bar", "not").WithParameters(dict),
            "bar=baz&foo=bar"
        );
    }
    #endregion

    private static void ExecuteQueryTest(Func<IDatasyncServiceClient<ClientKitchenSink>, IDatasyncQueryable<ClientKitchenSink>> linq, string expected)
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        IDatasyncQueryable<ClientKitchenSink> query = linq.Invoke(client);
        string actual = Uri.UnescapeDataString(query.ToODataQueryString());
        actual.Should().Be(expected);
    }

    private static void ExecuteUnsupportedQueryTest<TException>(Func<IDatasyncServiceClient<ClientKitchenSink>, IDatasyncQueryable<ClientKitchenSink>> linq) where TException : Exception
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        Action act = () => linq.Invoke(client).ToODataQueryString();
        act.Should().Throw<TException>();
    }

    private DatasyncServiceClient<T> GetMockClient<T>() where T : class
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        string tableName = typeof(T).Name.ToLowerInvariant();
        if (tableName.StartsWith("client"))
        {
            tableName = tableName["client".Length..];
        }

        HttpClientOptions options = new() 
        {
            Endpoint = new Uri("http://localhost"),
            HttpPipeline = [this.mockHandler]
        };

        HttpClientFactory factory = new(options);
        HttpClient client = factory.CreateClient();
        DatasyncServiceClient<T> serviceClient = new(new Uri(options.Endpoint, $"/tables/{tableName}"), client, serializerOptions);
        return serviceClient;
    }

    private static HttpResponseMessage GetSuccessfulResponse(ClientKitchenSink entity, HttpStatusCode code = HttpStatusCode.OK)
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        string json = JsonSerializer.Serialize(entity, serializerOptions);

        HttpResponseMessage response = new(code)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (entity.Id != null)
        {
            response.Headers.Location = new Uri($"http://localhost/tables/kitchensink/{entity.Id}");
        }

        if (entity.Version != null)
        {
            response.Headers.ETag = new EntityTagHeaderValue($"\"{entity.Version}\"");
        }

        return response;
    }

    private static HttpResponseMessage GetBadJsonResponse(HttpStatusCode code = HttpStatusCode.OK)
    {
        HttpResponseMessage response = new(code) { Content = new StringContent("{bad-json", Encoding.UTF8, "application/json") };
        return response;
    }

    internal class NamedSelectClass
    {
        public string Id { get; set; }
        public string StringValue { get; set; }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.mockHandler.Dispose();
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
