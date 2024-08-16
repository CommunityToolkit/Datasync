// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using System.Net;
using System.Text.Json;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class ExecutableOperation_Tests
{
    [Fact]
    public async Task CreateAsync_InvalidKind()
    {
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = (OperationKind)9999,
            State = OperationState.Pending,
            EntityType = string.Empty,
            ItemId = string.Empty,
            EntityVersion = string.Empty,
            Item = string.Empty,
            Sequence = 0,
            Version = 0
        };

        Func<Task> act = async () => _ = await ExecutableOperation.CreateAsync(op);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(null, "https://test.zumo.com/tables/movies", "https://test.zumo.com/tables/movies/")]
    [InlineData(null, "https://test.zumo.com/tables/movies/", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com", "/tables/movies", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com", "/tables/movies/", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com/", "/tables/movies", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com/", "/tables/movies/", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com/tables", "movies", "https://test.zumo.com/movies/")]
    [InlineData("https://test.zumo.com/tables", "movies/", "https://test.zumo.com/movies/")]
    [InlineData("https://test.zumo.com/tables", "/api/movies", "https://test.zumo.com/api/movies/")]
    [InlineData("https://test.zumo.com/tables", "/api/movies/", "https://test.zumo.com/api/movies/")]
    public void MakeAbsoluteUri_Works(string ba, string bb, string expected)
    {
        Uri arg1 = string.IsNullOrEmpty(ba) ? null : new Uri(ba, UriKind.Absolute);
        Uri arg2 = bb.StartsWith("http") ? new Uri(bb, UriKind.Absolute) : new Uri(bb, UriKind.Relative);
        Uri actual = ExecutableOperation.MakeAbsoluteUri(arg1, arg2);

        actual.ToString().Should().Be(expected);
    }

    [Fact]
    public void MakeAbsoluteUri_BaseAddressRelative()
    {
        Uri arg1 = new("tables/movies", UriKind.Relative);
        Uri arg2 = new("tables/movies", UriKind.Relative);

        Action act = () => ExecutableOperation.MakeAbsoluteUri(arg1, arg2);
        act.Should().Throw<UriFormatException>();
    }

    [Fact]
    public async Task AddOperation_ExecuteAsync()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net")};
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Add,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = itemJson,
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        string expectedJson = DatasyncSerializer.Serialize(expected);
        handler.AddResponse(HttpStatusCode.Created, expected);

        ExecutableOperation operation = await ExecutableOperation.CreateAsync(op);
        ServiceResponse response = await operation.ExecuteAsync(client, new Uri("/tables/movies", UriKind.Relative));

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/");
        (await request.Content.ReadAsStringAsync()).Should().Be(itemJson);

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(201);
        string content = new StreamReader(response.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);
    }

    [Fact]
    public async Task RemoveOperation_ExecuteAsync()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net") };
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Delete,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = itemJson,
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        handler.AddResponse(HttpStatusCode.NoContent);

        ExecutableOperation operation = await ExecutableOperation.CreateAsync(op);
        ServiceResponse response = await operation.ExecuteAsync(client, new Uri("/tables/movies", UriKind.Relative));

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/123");
        request.Should().NotHaveHeader("If-Match");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task RemoveOperation_ExecuteAsync_WithVersion()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net") };
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Delete,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = "abcdefg",
            Item = itemJson,
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        handler.AddResponse(HttpStatusCode.NoContent);

        ExecutableOperation operation = await ExecutableOperation.CreateAsync(op);
        ServiceResponse response = await operation.ExecuteAsync(client, new Uri("/tables/movies", UriKind.Relative));

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/123");
        request.Should().HaveHeader("If-Match", "\"abcdefg\"");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task ReplaceOperation_ExecuteAsync()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net") };
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Replace,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = itemJson,
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        string expectedJson = DatasyncSerializer.Serialize(expected);
        handler.AddResponse(HttpStatusCode.OK, expected);

        ExecutableOperation operation = await ExecutableOperation.CreateAsync(op);
        ServiceResponse response = await operation.ExecuteAsync(client, new Uri("/tables/movies", UriKind.Relative));

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/123");
        request.Should().NotHaveHeader("If-Match");
        (await request.Content.ReadAsStringAsync()).Should().Be(itemJson);

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(200);
        string content = new StreamReader(response.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);
    }

    [Fact]
    public async Task ReplaceOperation_ExecuteAsync_WithVersion()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net") };
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Replace,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = "abcdefg",
            Item = itemJson,
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        string expectedJson = DatasyncSerializer.Serialize(expected);
        handler.AddResponse(HttpStatusCode.OK, expected);

        ExecutableOperation operation = await ExecutableOperation.CreateAsync(op);
        ServiceResponse response = await operation.ExecuteAsync(client, new Uri("/tables/movies", UriKind.Relative));

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/123");
        request.Should().HaveHeader("If-Match", "\"abcdefg\"");
        (await request.Content.ReadAsStringAsync()).Should().Be(itemJson);

        response.Should().NotBeNull();
        response.HasContent.Should().BeTrue();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(200);
        string content = new StreamReader(response.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);
    }
}
