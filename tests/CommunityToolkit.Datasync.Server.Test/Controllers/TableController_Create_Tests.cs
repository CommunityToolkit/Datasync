// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CommunityToolkit.Datasync.Server.Test.Controllers;

[ExcludeFromCodeCoverage]
public class TableController_Create_Tests : BaseTest
{
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

        CreatedAtRouteResult actual = await controller.CreateAsync() as CreatedAtRouteResult;

        actual.Should().NotBeNull();
        actual.StatusCode.Should().Be(201);
        actual.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(entity.Id);

        await accessProvider.Received(1).PreCommitHookAsync(TableOperation.Create, Arg.Any<TableData>(), Arg.Any<CancellationToken>());
        await accessProvider.Received(1).PostCommitHookAsync(TableOperation.Create, Arg.Any<TableData>(), Arg.Any<CancellationToken>());
        firedEvents.Should().ContainSingle();

        await repository.Received(1).CreateAsync(Arg.Any<TableData>(), Arg.Any<CancellationToken>());
    }
}
