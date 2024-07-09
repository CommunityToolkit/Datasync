// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net;
using System.Text.Json;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Remove_Tests : BaseOperationTest
{
    private RemoteOperationOptions defaultOptions = new();

    [Fact]
    public async Task RemoveAsync_ThrowsOnNull()
    {
        Func<Task> act = async () => await Dataset.RemoveAsync(null, this.defaultOptions);
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
    public async Task RemoveAsync_ThrowsOnInvalidId(string id)
    {
        Func<Task> act = async () => await Dataset.RemoveAsync(id, this.defaultOptions);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory, CombinatorialData]
    public async Task RemoveAsync_FormulatesCorrectResponse(bool hasPrecondition)
    {
        MockHandler.AddResponse(HttpStatusCode.NoContent);
        RemoteOperationOptions options = new() { RequiredVersion = hasPrecondition ? "abcdefg" : null };
        await Dataset.RemoveAsync("1234", options);
        MockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = MockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}/1234");
        if (hasPrecondition)
        {
            request.Headers.IfMatch.Should().HaveCount(1);
            request.Headers.IfMatch.FirstOrDefault().Tag.Should().Be("\"abcdefg\"");
            request.Headers.IfMatch.FirstOrDefault().IsWeak.Should().BeFalse();
        }
        else
        {
            request.Headers.IfMatch.Should().HaveCount(0);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [Trait("Method", "RemoveAsync")]
    public async Task RemoveAsync_Conflict_FormulatesCorrectResponse(HttpStatusCode statusCode)
    {
        ClientMovie payload = new() { Id = "1", Version = "1234", UpdatedAt = DateTimeOffset.UnixEpoch };
        TestData.Movies.BlackPanther.CopyTo(payload);
        MockHandler.AddResponse(statusCode, payload);
        ConflictException<ClientMovie> ex = await Assert.ThrowsAsync<ConflictException<ClientMovie>>(() => Dataset.RemoveAsync("1", this.defaultOptions));
        ex.StatusCode.Should().Be(statusCode);
        ex.ContentType.Should().Be("application/json");
        ex.Payload.Should().NotBeNullOrEmpty();
        ex.ServerEntity.Should().BeEquivalentTo<IMovie>(payload);
        ex.ServerEntity.Id.Should().Be("1");
        ex.ServerEntity.Version.Should().Be("1234");
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [Trait("Method", "RemoveAsync")]
    public async Task RemoveAsync_ConflictNoContent_FormulatesCorrectResponse(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        async Task act() => await Dataset.RemoveAsync("1", this.defaultOptions);
        JsonException ex = await Assert.ThrowsAsync<JsonException>(act);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [Trait("Method", "RemoveAsync")]
    public async Task RemoveAsync_ConflictWithBadJson_Throws(HttpStatusCode statusCode)
    {
        MockHandler.AddResponseContent("{this-is-bad-json", statusCode);
        Func<Task> act = async () => await Dataset.RemoveAsync("1", this.defaultOptions);
        await act.Should().ThrowAsync<JsonException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [Trait("Method", "RemoveAsync")]
    public async Task RemoveAsync_RequestFailed_Throws(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        Func<Task> act = async () => await Dataset.RemoveAsync("1", this.defaultOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>()).Which.StatusCode.Should().Be(statusCode);
    }
}
