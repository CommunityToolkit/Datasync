// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Models;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Count_Tests : BaseOperationTest
{
    private RemoteOperationOptions defaultOptions = new();

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.UnavailableForLegalReasons)]
    [InlineData(HttpStatusCode.ExpectationFailed)]
    [Trait("Method", "CountItemsAsync")]
    public async Task CountItemsAsync_Throws_OnBadRequest(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        Func<Task> act = async () => await Dataset.CountAsync(string.Empty, this.defaultOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>())
            .Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    [Trait("Method", "CountItemsAsync")]
    public async Task CountItemsAsync_NoItems_WithFilter()
    {
        MockHandler.AddResponse(HttpStatusCode.OK, new Page<ClientMovie>() { Count = 0L });

        long count = await Dataset.CountAsync("$filter=(stringField eq 'id')", this.defaultOptions);

        count.Should().Be(0);

        HttpRequestMessage request = MockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}?$filter=(stringField+eq+%27id%27)&$select=id&$skip=0&$top=1&$count=true");
    }

    [Fact]
    [Trait("Method", "CountItemsAsync")]
    public async Task CountItemsAsync_NoItems_WithFilter_IncludeDeleted()
    {
        MockHandler.AddResponse(HttpStatusCode.OK, new Page<ClientMovie>() { Count = 0L });
        RemoteOperationOptions options = new() { IncludeDeletedItems = true };

        long count = await Dataset.CountAsync("$filter=(stringField eq 'id')", options);

        count.Should().Be(0);

        HttpRequestMessage request = MockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}?$filter=(stringField+eq+%27id%27)&$select=id&$skip=0&$top=1&$count=true&__includedeleted=true");
    }

    [Fact]
    [Trait("Method", "CountItemsAsync")]
    public async Task CountItemsAsync_NoCount()
    {
        MockHandler.AddResponse(HttpStatusCode.OK, new Page<ClientMovie>());
        Func<Task> act = async () => await Dataset.CountAsync(string.Empty, this.defaultOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    [Trait("Method", "CountItemsAsync")]
    public async Task CountItemsAsync_Count()
    {
        MockHandler.AddResponse(HttpStatusCode.OK, new Page<ClientMovie>() { Count = 42 });

        long count = await Dataset.CountAsync(string.Empty, this.defaultOptions);

        count.Should().Be(42);
        HttpRequestMessage request = MockHandler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}?$select=id&$skip=0&$top=1&$count=true");
    }
}
