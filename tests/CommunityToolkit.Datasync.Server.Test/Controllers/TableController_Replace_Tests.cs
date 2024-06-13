// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CommunityToolkit.Datasync.Server.Test.Controllers;

[ExcludeFromCodeCoverage]
public class TableController_Replace_Tests : BaseTest
{
    [Fact]
    public async Task ReplaceAsync_IdMismatch_Throws()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, true);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}", entity);

        Func<Task> act = async () => await controller.ReplaceAsync("1");

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
    }

    [Fact]
    public async Task ReplaceAsync_NonJson_Throws()
    {
        TableData entity = new() { Id = "cac9b793-000c-450e-8477-663522b14727" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, true);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateNonJsonHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}");

        Func<Task> act = async () => await controller.ReplaceAsync(entity.Id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(415);
    }

    [Fact]
    public async Task ReplaceAsync_RepositoryException_Throws()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, true);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}", entity);

        Func<Task> act = async () => await controller.ReplaceAsync(entity.Id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(404);
    }

    [Fact]
    public async Task ReplaceAsync_EntityNotInView_Throws()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, true, m => m.Id == "1");
        IRepository<TableData> repository = FakeRepository(entity, false);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}", entity);

        Func<Task> act = async () => await controller.ReplaceAsync(entity.Id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(404);
    }

    [Fact]
    public async Task ReplaceAsync_Unauthorized_Throws()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, false);
        IRepository<TableData> repository = FakeRepository(entity, false);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}", entity);

        Func<Task> act = async () => await controller.ReplaceAsync(entity.Id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(401);
    }

    [Fact]
    public async Task ReplaceAsync_SoftDeleted_Throws()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4", Deleted = true };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, true);
        IRepository<TableData> repository = FakeRepository(entity, false);
        TableControllerOptions options = new() { EnableSoftDelete = true };
        ExposedTableController<TableData> controller = new(repository, accessProvider, options);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}", entity);

        Func<Task> act = async () => await controller.ReplaceAsync(entity.Id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(410);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ReplaceAsync_PreconditionFailed_Throws(bool includeIfMatch, bool includeLastModified)
    {
        TableData entity = new()
        {
            Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4",
            Version = [0x61, 0x62, 0x63, 0x64, 0x65],
            UpdatedAt = new DateTimeOffset(2023, 11, 13, 12, 30, 05, TimeSpan.Zero),
            Deleted = false
        };

        Dictionary<string, string> headers = [];
        if (includeIfMatch)
        {
            headers.Add("If-Match", "\"foo\"");
        }

        if (includeLastModified)
        {
            headers.Add("If-Unmodified-Since", "Sun, 12 Nov 2023 07:28:00 GMT");
        }

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, true);
        TableControllerOptions options = new();
        IRepository<TableData> repository = FakeRepository(entity, false);
        ExposedTableController<TableData> controller = new(repository, accessProvider, options);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}", entity, headers);

        Func<Task> act = async () => await controller.ReplaceAsync(entity.Id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(412).And.WithPayload(entity);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ReplaceAsync_Works(bool includeIfMatch, bool includeLastModified)
    {
        TableData entity = new()
        {
            Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4",
            Version = [0x61, 0x62, 0x63, 0x64, 0x65],
            UpdatedAt = new DateTimeOffset(2023, 11, 13, 12, 30, 05, TimeSpan.Zero),
            Deleted = false
        };

        Dictionary<string, string> headers = [];
        if (includeIfMatch)
        {
            headers.Add("If-Match", $"\"{Convert.ToBase64String(entity.Version)}\"");
        }

        if (includeLastModified)
        {
            headers.Add("If-Unmodified-Since", "Wed, 15 Nov 2023 07:28:00 GMT");
        }

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Update, true);
        TableControllerOptions options = new();
        IRepository<TableData> repository = FakeRepository(entity, false);
        ExposedTableController<TableData> controller = new(repository, accessProvider, options);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Put, $"https://localhost/table/{entity.Id}", entity, headers);
        List<RepositoryUpdatedEventArgs> firedEvents = [];
        controller.RepositoryUpdated += (_, e) => firedEvents.Add(e);

        OkObjectResult actual = await controller.ReplaceAsync(entity.Id) as OkObjectResult;

        actual.Should().NotBeNull();
        actual.StatusCode.Should().Be(200);
        actual.Value.Should().BeEquivalentTo(entity);

        await accessProvider.Received(1).PreCommitHookAsync(TableOperation.Update, Arg.Any<TableData>(), Arg.Any<CancellationToken>());
        await accessProvider.Received(1).PostCommitHookAsync(TableOperation.Update, Arg.Any<TableData>(), Arg.Any<CancellationToken>());
        firedEvents.Should().ContainSingle();

        await repository.Received(1).ReadAsync(entity.Id, Arg.Any<CancellationToken>());
        await repository.Received(1).ReplaceAsync(Arg.Any<TableData>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }
}
