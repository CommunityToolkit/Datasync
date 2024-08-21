// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Offline.Operations;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using System.Net;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline.Internal;

[ExcludeFromCodeCoverage]
public class AddOperation_Tests
{
    [Fact]
    public async Task AddOperation_ExecuteAsync()
    {
        MockDelegatingHandler handler = new();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://test.zumo.net") };
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
}
