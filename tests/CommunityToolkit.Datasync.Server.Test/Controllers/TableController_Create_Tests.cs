// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CommunityToolkit.Datasync.Server.Test.Controllers;

[ExcludeFromCodeCoverage]
public class TableController_Create_Tests : BaseTest
{
    [Fact]
    public async Task CreateAsync_UnsafeEntityLogging_False_LogsIdOnly()
    {
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, false);
        TableControllerOptions options = new() { UnsafeEntityLogging = false };
        CapturingLogger logger = new();
        ExposedTableController<TableData> controller = new(repository, accessProvider, options) { Logger = logger };
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Post, "https://localhost/table", entity);

        _ = await controller.CreateAsync(TestContext.Current.CancellationToken);

        logger.Entries.Should().Contain(e => e.LogLevel == LogLevel.Information && e.Message.Contains(entity.Id));
        logger.Entries.Should().NotContain(e => e.Message.Contains("UpdatedAt", StringComparison.OrdinalIgnoreCase));
        logger.Entries.Should().NotContain(e => e.LogLevel == LogLevel.Debug);
    }

    [Fact]
    public async Task CreateAsync_UnsafeEntityLogging_True_LogsFullEntityAtDebug()
    {
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, false);
        TableControllerOptions options = new() { UnsafeEntityLogging = true };
        CapturingLogger logger = new();
        ExposedTableController<TableData> controller = new(repository, accessProvider, options) { Logger = logger };
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Post, "https://localhost/table", entity);

        _ = await controller.CreateAsync(TestContext.Current.CancellationToken);

        logger.Entries.Should().Contain(e => e.LogLevel == LogLevel.Information && e.Message.Contains(entity.Id));
        logger.Entries.Should().Contain(e => e.LogLevel == LogLevel.Debug && e.Message.Contains("UpdatedAt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_Unauthorized_Throws()
    {
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, false);
        IRepository<TableData> repository = FakeRepository<TableData>();
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        TableData entity = new();
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Post, "https://localhost/table", entity);

        Func<Task> act = async () => await controller.CreateAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(401);
    }

    [Fact]
    public async Task CreateAsync_RepositoryException_Throws()
    {
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, true);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        TableData entity = new();
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Post, "https://localhost/table", entity);

        Func<Task> act = async () => await controller.CreateAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409);
    }

    [Fact]
    public async Task CreateAsync_Conflict_EntityInView_Returns409()
    {
        TableData conflictingEntity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };
        // The data view includes the conflicting entity, so the conflict should be surfaced as a 409.
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, true, x => x.Id == conflictingEntity.Id);
        IRepository<TableData> repository = FakeRepository<TableData>(null, true, conflictingEntity);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        TableData entity = new() { Id = conflictingEntity.Id };
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Post, "https://localhost/table", entity);

        Func<Task> act = async () => await controller.CreateAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(409);
    }

    [Fact]
    public async Task CreateAsync_Conflict_EntityNotInView_Returns400()
    {
        TableData conflictingEntity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };
        // The data view excludes the conflicting entity, so the conflict must NOT be surfaced - return 400 instead.
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, true, x => x.Id == "some-other-id");
        IRepository<TableData> repository = FakeRepository<TableData>(null, true, conflictingEntity);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        TableData entity = new() { Id = conflictingEntity.Id };
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Post, "https://localhost/table", entity);

        Func<Task> act = async () => await controller.CreateAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400).And.Which.Payload.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_NonJsonData_Throws()
    {
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, true);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateNonJsonHttpContext(HttpMethod.Post, "https://localhost/table");

        Func<Task> act = async () => await controller.CreateAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(415);
    }

    [Fact]
    public async Task CreateAsync_Works()
    {
        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, false);
        ExposedTableController<TableData> controller = new(repository, accessProvider);
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Post, "https://localhost/table", entity);
        List<RepositoryUpdatedEventArgs> firedEvents = [];
        controller.RepositoryUpdated += (_, e) => firedEvents.Add(e);

        CreatedAtRouteResult actual = await controller.CreateAsync(TestContext.Current.CancellationToken) as CreatedAtRouteResult;

        actual.Should().NotBeNull();
        actual.StatusCode.Should().Be(201);
        actual.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(entity.Id);

        await accessProvider.Received(1).PreCommitHookAsync(TableOperation.Create, Arg.Any<TableData>(), Arg.Any<CancellationToken>());
        await accessProvider.Received(1).PostCommitHookAsync(TableOperation.Create, Arg.Any<TableData>(), Arg.Any<CancellationToken>());
        firedEvents.Should().ContainSingle();

        await repository.Received(1).CreateAsync(Arg.Any<TableData>(), Arg.Any<CancellationToken>());
    }
}
