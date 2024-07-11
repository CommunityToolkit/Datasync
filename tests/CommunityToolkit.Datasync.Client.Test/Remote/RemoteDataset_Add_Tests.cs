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
public class RemoteDataset_Add_Tests : BaseOperationTest
{
    private static ClientMovie CreateMockMovie(string id)
    {
        ClientMovie movie = new() { Id = id, Version = "1234", UpdatedAt = DateTimeOffset.UnixEpoch };
        TestData.Movies.BlackPanther.CopyTo(movie);
        return movie;
    }

    [Fact]
    public async Task AddAsync_ThrowsOnNull()
    {
        Func<Task> act = async () => await Dataset.AddAsync(null, DefaultOperationOptions);
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
    public async Task AddAsync_ThrowsOnInvalidId(string id)
    {
        ClientMovie payload = CreateMockMovie(id);
        Func<Task> act = async () => await Dataset.AddAsync(payload, DefaultOperationOptions);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    public async Task AddAsync_Success_Id(HttpStatusCode statusCode)
    {
        ClientMovie payload = CreateMockMovie("1");
        MockHandler.AddResponse(statusCode, payload);

        ClientMovie submission = CreateMockMovie("42");
        ClientMovie actual = await Dataset.AddAsync(submission, DefaultOperationOptions);

        actual.Should().BeEquivalentTo<IMovie>(payload);
        actual.Id.Should().Be(payload.Id);
        actual.Version.Should().Be(payload.Version);
        actual.UpdatedAt.Should().Be(payload.UpdatedAt);

        MockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = MockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}");
        string jsonContent = await request.Content.ReadAsStringAsync();
        string submissionContent = JsonSerializer.Serialize(submission, this.serializerOptions);
        jsonContent.Should().Be(submissionContent);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    public async Task AddAsync_Success_NullId(HttpStatusCode statusCode)
    {
        ClientMovie payload = CreateMockMovie("1");
        MockHandler.AddResponse(statusCode, payload);

        ClientMovie submission = CreateMockMovie(null);

        ClientMovie actual = await Dataset.AddAsync(submission, DefaultOperationOptions);

        actual.Should().BeEquivalentTo<IMovie>(payload);
        actual.Id.Should().Be(payload.Id);
        actual.Version.Should().Be(payload.Version);
        actual.UpdatedAt.Should().Be(payload.UpdatedAt);

        MockHandler.Requests.Should().HaveCount(1);
        HttpRequestMessage request = MockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.ToString().Should().Be($"{BaseAddress}{Path}");
        string jsonContent = await request.Content.ReadAsStringAsync();
        string submissionContent = JsonSerializer.Serialize(submission, this.serializerOptions);
        jsonContent.Should().Be(submissionContent);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    public async Task AddAsync_SuccessNoContent(HttpStatusCode statusCode)
    {
        ClientMovie submission = CreateMockMovie("42");
        MockHandler.AddResponse(statusCode);

        Func<Task> act = async () => await Dataset.AddAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    public async Task AddAsync_SuccessWithBadJson_Throws(HttpStatusCode statusCode)
    {
        ClientMovie submission = CreateMockMovie("42");
        MockHandler.AddResponseContent("{this-is-bad-json", statusCode);

        Func<Task> act = async () => await Dataset.AddAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task AddAsync_Conflict_FormulatesCorrectResponse(HttpStatusCode statusCode)
    {
        ClientMovie payload = CreateMockMovie("1");
        MockHandler.AddResponse(statusCode, payload);

        ClientMovie submission = CreateMockMovie("42");
        Func<Task> act = async () => await Dataset.AddAsync(submission, DefaultOperationOptions);

        ExceptionAssertions<ConflictException<ClientMovie>> ex = await act.Should().ThrowAsync<ConflictException<ClientMovie>>();
        ex.Which.StatusCode.Should().Be(statusCode);
        ex.Which.Payload.Should().NotBeNullOrEmpty();
        ex.Which.ServerEntity.Should().BeEquivalentTo(payload);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task AddAsync_ConflictNoContent_Throws(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        ClientMovie submission = CreateMockMovie("42");
        Func<Task> act = async () => await Dataset.AddAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task AddAsync_ConflictWithBadJson_Throws(HttpStatusCode statusCode)
    {
        ClientMovie submission = CreateMockMovie("42");
        MockHandler.AddResponseContent("{this-is-bad-json", statusCode);
        Func<Task> act = async () => await Dataset.AddAsync(submission, DefaultOperationOptions);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AddAsync_RequestFailed_Throws(HttpStatusCode statusCode)
    {
        MockHandler.AddResponse(statusCode);
        ClientMovie submission = CreateMockMovie("42");
        Func<Task> act = async () => await Dataset.AddAsync(submission, DefaultOperationOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>())
            .Which.StatusCode.Should().Be(statusCode);
    }
}
