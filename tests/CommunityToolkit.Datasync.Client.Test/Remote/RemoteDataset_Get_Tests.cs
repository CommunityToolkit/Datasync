// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Get_Tests : BaseOperationTest
{
    [Theory, CombinatorialData]
    public async Task GetAsync_ThrowsOnNull(bool includeDeleted)
    {
        RemoteOperationOptions options = new() { IncludeDeletedItems = includeDeleted };
        Func<Task> act = async () => _ = await Dataset.GetAsync(null, options);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("abcdef gh")]
    [InlineData("!!!")]
    [InlineData("?")]
    [InlineData(";")]
    [InlineData("{EA235ADF-9F38-44EA-8DA4-EF3D24755767}")]
    [InlineData("###")]
    public async Task GetAsync_ThrowsOnInvalidId(string id)
    {
        Func<Task> act = async () => _ = await Dataset.GetAsync(id, DefaultOperationOptions);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAsync_SuccessNoContent()
    {
        MockHandler.AddResponse(HttpStatusCode.OK);
        Func<Task> act = async () => _ = await Dataset.GetAsync("1", DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    public async Task GetAsync_SuccessBadJson()
    {
        MockHandler.AddResponseContent("{this-is-bad-json");
        Func<Task> act = async () => _ = await Dataset.GetAsync("1", DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotModified)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task GetAsync_RequestFailed_Throws(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        Func<Task> act = async () => _ = await Dataset.GetAsync("1", DefaultOperationOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>())
            .Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task GetAsync_NotFound_Throws()
    {
        MockHandler.AddResponse(HttpStatusCode.NotFound);
        Func<Task> act = async () => _ = await Dataset.GetAsync("1", DefaultOperationOptions);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetAsync_FormulatesCorrectRequest()
    {
        ClientMovie payload = new() { Id = "1", Version = "1234", UpdatedAt = DateTimeOffset.UnixEpoch };
        TestData.Movies.BlackPanther.CopyTo(payload);

        MockHandler.AddResponse(HttpStatusCode.OK, payload);

        ClientMovie actual = await Dataset.GetAsync("1", DefaultOperationOptions);
        actual.Should().BeEquivalentTo<IMovie>(payload);
        actual.Id.Should().Be(payload.Id);
        actual.Version.Should().Be(payload.Version);
        actual.UpdatedAt.Should().Be(payload.UpdatedAt);

        MockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = MockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}/1");
    }

    [Fact]
    public async Task GetAsync_Extension_FormulatesCorrectRequest()
    {
        ClientMovie payload = new() { Id = "1", Version = "1234", UpdatedAt = DateTimeOffset.UnixEpoch };
        TestData.Movies.BlackPanther.CopyTo(payload);

        MockHandler.AddResponse(HttpStatusCode.OK, payload);

        ClientMovie actual = await Dataset.GetAsync("1");
        actual.Should().BeEquivalentTo<IMovie>(payload);
        actual.Id.Should().Be(payload.Id);
        actual.Version.Should().Be(payload.Version);
        actual.UpdatedAt.Should().Be(payload.UpdatedAt);

        MockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = MockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}/1");
    }

    [Fact]
    public async Task GetAsync_FormulatesCorrectRequest_IncludeDeleted()
    {
        RemoteOperationOptions options = new() { IncludeDeletedItems = true };
        ClientMovie payload = new() { Id = "1", Version = "1234", UpdatedAt = DateTimeOffset.UnixEpoch };
        TestData.Movies.BlackPanther.CopyTo(payload);

        MockHandler.AddResponse(HttpStatusCode.OK, payload);

        ClientMovie actual = await Dataset.GetAsync("1", options);
        actual.Should().BeEquivalentTo<IMovie>(payload);
        actual.Id.Should().Be(payload.Id);
        actual.Version.Should().Be(payload.Version);
        actual.UpdatedAt.Should().Be(payload.UpdatedAt);

        MockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = MockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}/1?__includedeleted=true");
    }
}
