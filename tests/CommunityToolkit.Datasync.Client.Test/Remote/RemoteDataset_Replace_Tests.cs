// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using FluentAssertions.Specialized;
using System.Net;
using System.Text.Json;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Replace_Tests : BaseOperationTest
{
    private static ClientMovie CreateMockMovie(string id)
    {
        ClientMovie movie = new() { Id = id, Version = "1234", UpdatedAt = DateTimeOffset.UnixEpoch };
        TestData.Movies.BlackPanther.CopyTo(movie);
        return movie;
    }

    [Fact]
    public async Task ReplaceAsync_ThrowsOnNull()
    {
        Func<Task> act = async () => await Dataset.ReplaceAsync(null, DefaultOperationOptions);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReplaceAsync_ThrowsOnNullId()
    {
        ClientMovie submission = CreateMockMovie(null);
        Func<Task> act = async () => await Dataset.ReplaceAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<ArgumentException>();
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
    public async Task ReplaceAsync_ThrowsOnInvalidId(string id)
    {
        ClientMovie submission = CreateMockMovie(id);
        Func<Task> act = async () => await Dataset.ReplaceAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory, CombinatorialData]
    public async Task ReplaceAsync_Success_FormulatesCorrectResponse(bool hasPrecondition)
    {
        const string id = "42";
        ClientMovie payload = CreateMockMovie(id); payload.Version = "abcd";
        ClientMovie submission = CreateMockMovie(id); submission.Version = "1234";
        MockHandler.AddResponse(HttpStatusCode.OK, payload);
        RemoteOperationOptions options = new();
        if (hasPrecondition)
        {
            options.RequiredVersion = submission.Version;
        }

        ClientMovie actual = await Dataset.ReplaceAsync(submission, options);

        actual.Should().BeEquivalentTo<IMovie>(payload);
        actual.Id.Should().Be(payload.Id);
        actual.Version.Should().Be(payload.Version);
        actual.UpdatedAt.Should().Be(payload.UpdatedAt);

        MockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = MockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}/{id}");
        string jsonContent = await request.Content.ReadAsStringAsync();
        string submissionContent = JsonSerializer.Serialize(submission, this.serializerOptions);
        jsonContent.Should().Be(submissionContent);
        if (hasPrecondition)
        {
            request.Headers.IfMatch.Should().HaveCount(1);
            request.Headers.IfMatch.FirstOrDefault().Tag.Should().Be("\"1234\"");
            request.Headers.IfMatch.FirstOrDefault().IsWeak.Should().BeFalse();
        }
        else
        {
            request.Headers.IfMatch.Should().HaveCount(0);
        }
    }

    [Fact]
    public async Task ReplaceAsync_SuccessNoContent()
    {
        ClientMovie submission = CreateMockMovie("42");
        MockHandler.AddResponse(HttpStatusCode.OK);

        Func<Task> act = async () => await Dataset.ReplaceAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task ReplaceAsync_Conflict_FormulatesCorrectResponse(HttpStatusCode statusCode)
    {
        ClientMovie payload = CreateMockMovie("1");
        MockHandler.AddResponse(statusCode, payload);

        ClientMovie submission = CreateMockMovie("42");
        Func<Task> act = async () => await Dataset.ReplaceAsync(submission, DefaultOperationOptions);

        ExceptionAssertions<ConflictException<ClientMovie>> ex = await act.Should().ThrowAsync<ConflictException<ClientMovie>>();
        ex.Which.StatusCode.Should().Be(statusCode);
        ex.Which.Payload.Should().NotBeNullOrEmpty();
        ex.Which.ServerEntity.Should().BeEquivalentTo(payload);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task ReplaceAsync_ConflictNoContent(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        ClientMovie submission = CreateMockMovie("42");
        Func<Task> act = async () => await Dataset.ReplaceAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task ReplaceAsync_ConflictWithBadJson_Throws(HttpStatusCode statusCode)
    {
        ClientMovie submission = CreateMockMovie("42");
        MockHandler.AddResponseContent("{this-is-bad-json", statusCode);
        Func<Task> act = async () => await Dataset.ReplaceAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReplaceAsync_RequestFailed_Throws(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        ClientMovie submission = CreateMockMovie("42");
        Func<Task> act = async () => await Dataset.ReplaceAsync(submission, DefaultOperationOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>())
            .Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task ReplaceAsync_NotFound_Throws()
    {
        MockHandler.AddResponse(HttpStatusCode.NotFound);
        ClientMovie submission = CreateMockMovie("42");
        Func<Task> act = async () => _ = await Dataset.ReplaceAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
