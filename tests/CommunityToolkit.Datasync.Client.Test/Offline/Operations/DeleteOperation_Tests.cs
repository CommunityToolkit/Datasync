// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Offline.Operations;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using System.Net;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline.Operations;

[ExcludeFromCodeCoverage]
public class DeleteOperation_Tests
{
    [Fact]
    public async Task DeleteOperation_ExecuteAsync()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net") };
        const string itemJson = """{"id":"123"}""";
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

        EntityDatasyncOptions options = new()
        {
            HttpClient = client,
            Endpoint = new Uri("/tables/movies", UriKind.Relative),
            QueryDescription = new()
        };
        ExecutableOperation operation = await ExecutableOperation.CreateAsync(op);
        ServiceResponse response = await operation.ExecuteAsync(options);

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/123");
        request.Headers.Should().NotContain(x => x.Key == "If-Match");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteOperation_ExecuteAsync_WithVersion()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net") };
        const string itemJson = """{"id":"123"}""";
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

        EntityDatasyncOptions options = new()
        {
            HttpClient = client,
            Endpoint = new Uri("/tables/movies", UriKind.Relative),
            QueryDescription = new()
        };
        ExecutableOperation operation = await ExecutableOperation.CreateAsync(op);
        ServiceResponse response = await operation.ExecuteAsync(options);

        HttpRequestMessage request = handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/123");
        request.Headers.Should().Contain(x => x.Key == "If-Match" && x.Value.First() == "\"abcdefg\"");

        response.Should().NotBeNull();
        response.HasContent.Should().BeFalse();
        response.IsConflictStatusCode.Should().BeFalse();
        response.IsSuccessful.Should().BeTrue();
        response.ReasonPhrase.Should().NotBeNullOrEmpty();
        response.StatusCode.Should().Be(204);
    }
}
