// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OperationsQueueManager_Tests : BaseTest
{
    #region GetExistingOperationAsync
    [Fact]
    public async Task GetExistingOperationAsync_InvalidId_Throws()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager manager = context.QueueManager;
        ClientMovie movie = new() { Id = "###" };
        Func<Task> act = async () => _ = await manager.GetExistingOperationAsync(movie);
        await act.Should().ThrowAsync<DatasyncException>();
    }
    #endregion

    #region InitializeEntityMap
    [Fact]
    public void InitializeDatasyncEntityMap_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        sut.InitializeEntityMap();

        Dictionary<string, Type> expected = new()
        {
            { typeof(ClientMovie).FullName, typeof(ClientMovie) },
            { typeof(Entity3).FullName, typeof(Entity3) }
        };

        sut.EntityMap.Should().NotBeNullOrEmpty().And.BeEquivalentTo(expected);
    }
    #endregion

    #region NullAsEmpty
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("foo", "foo")]
    public void NullAsEmpty_Works(string value, string expected)
    {
        OperationsQueueManager.NullAsEmpty(value).Should().Be(expected);
    }
    #endregion

    #region PushOperationAsync(DatasyncOperation)
    [Fact]
    public async Task PushOperationAsync_Null()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;

        Func<Task> act = async () => _ = await sut.PushOperationAsync(null);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PushOperationAsync_Addition()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Add,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = """{"id":"123"}""",
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        string expectedJson = JsonSerializer.Serialize(expected, DatasyncSerializer.JsonSerializerOptions);
        context.Handler.Responses.Add(GetResponse(expectedJson, HttpStatusCode.Created));

        ServiceResponse response = await sut.PushOperationAsync(op);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
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
    public async Task PushOperationAsync_Deletion()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Delete,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = """{"id":"123"}""",
            Sequence = 0,
            Version = 0
        };

        context.Handler.AddResponse(HttpStatusCode.NoContent);

        ServiceResponse response = await sut.PushOperationAsync(op);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
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
    public async Task PushOperationAsync_Deletion_WithVersion()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Delete,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = "abcdefg",
            Item = """{"id":"123"}""",
            Sequence = 0,
            Version = 0
        };

        context.Handler.AddResponse(HttpStatusCode.NoContent);

        ServiceResponse response = await sut.PushOperationAsync(op);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
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
    public async Task PushOperationAsync_Replacement()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Replace,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = """{"id":"123"}""",
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        string expectedJson = JsonSerializer.Serialize(expected, DatasyncSerializer.JsonSerializerOptions);
        context.Handler.Responses.Add(GetResponse(expectedJson, HttpStatusCode.OK));

        ServiceResponse response = await sut.PushOperationAsync(op);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
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
    public async Task PushOperationAsync_Replacement_WithVersion()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Replace,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = "abcdefg",
            Item = """{"id":"123"}""",
            Sequence = 0,
            Version = 0
        };

        ClientMovie expected = new() { Id = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(expected);
        string expectedJson = JsonSerializer.Serialize(expected, DatasyncSerializer.JsonSerializerOptions);
        context.Handler.Responses.Add(GetResponse(expectedJson, HttpStatusCode.OK));

        ServiceResponse response = await sut.PushOperationAsync(op);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
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
    #endregion

    #region ToOperationKind
    [Fact]
    public void ToOperationKind_Invalid_Throws()
    {
        EntityState sut = EntityState.Detached;
        Action act = () => _ = OperationsQueueManager.ToOperationKind(sut);
        act.Should().Throw<InvalidOperationException>();
    }
    #endregion

    #region IDisposable
    [Fact]
    public void Dispose_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;

        sut.Dispose();
        sut.Dispose();
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CheckDisposed_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;

        Action act = () => sut.CheckDisposed();
        act.Should().NotThrow();
    }
    #endregion
}
