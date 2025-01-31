// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.InMemory;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace CommunityToolkit.Datasync.Server.Test.Controllers;

[ExcludeFromCodeCoverage]
public class TableController_Query_Tests : BaseTest
{
    #region BuildPagedResult
    [Fact]
    public void BuildPagedResult_NulLArg_BuildsPagedResult()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };

        // Build an ODataQueryOptions with no query
        ODataConventionModelBuilder builder = new();
        builder.AddEntityType(typeof(InMemoryMovie));
        IEdmModel model = builder.GetEdmModel();
        ODataQueryContext queryContext = new(model, typeof(InMemoryMovie), new Microsoft.OData.UriParser.ODataPath());
        DefaultHttpContext httpContext = new();
        httpContext.Request.Method = "GET";
        ODataQueryOptions queryOptions = new(queryContext, httpContext.Request);

        PagedResult result = controller.BuildPagedResult(queryOptions, null, 0);
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(0);
        result.Count.Should().BeNull();
    }
    #endregion

    #region QueryAsync
    [Fact]
    public async Task QueryAsync_Unauthorized_Throws()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Query, false);
        IRepository<TableData> repository = FakeRepository(entity, true);
        TableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Get, "https://localhost/table");

        Func<Task> act = async () => await controller.QueryAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(401);
    }

    [Fact]
    public async Task QueryAsync_RepositoryException_Throws()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Query, true);
        IRepository<TableData> repository = FakeRepository<TableData>(null, true);
        TableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Get, "https://localhost/table");

        Func<Task> act = async () => await controller.QueryAsync();

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(500);
    }

    [Fact]
    public async Task QueryAsync_NoExtras_Works()
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Query, true);
        IRepository<TableData> repository = FakeRepository(entity, true);
        TableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Get, "https://localhost/table");

        OkObjectResult result = await controller.QueryAsync() as OkObjectResult;
        result.Should().NotBeNull();
        PagedResult pagedResult = result.Value as PagedResult;
        pagedResult.Should().NotBeNull();
        pagedResult.Items.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("0da7fb24-3606-442f-9f68-c47c6e7d09d4", 1)]
    [InlineData("1", 0)]
    public async Task QueryAsync_DataView_Works(string filter, int count)
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4" };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Query, true, m => m.Id == filter);
        IRepository<TableData> repository = FakeRepository(entity, true);
        TableController<TableData> controller = new(repository, accessProvider);
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Get, "https://localhost/table");

        OkObjectResult result = await controller.QueryAsync() as OkObjectResult;
        result.Should().NotBeNull();
        PagedResult pagedResult = result.Value as PagedResult;
        pagedResult.Should().NotBeNull();
        pagedResult.Items.Should().HaveCount(count);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public async Task QueryAsync_DeletedSkipped_Works(bool isDeleted, int count)
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4", Deleted = isDeleted };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Query, true);
        IRepository<TableData> repository = FakeRepository(entity, true);
        TableControllerOptions options = new() { EnableSoftDelete = true };
        TableController<TableData> controller = new(repository, accessProvider) { Options = options };
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Get, "https://localhost/table");

        OkObjectResult result = await controller.QueryAsync() as OkObjectResult;
        result.Should().NotBeNull();
        PagedResult pagedResult = result.Value as PagedResult;
        pagedResult.Should().NotBeNull();
        pagedResult.Items.Should().HaveCount(count);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    public async Task QueryAsync_DeletedIncluded_Works(bool isDeleted, int count)
    {
        TableData entity = new() { Id = "0da7fb24-3606-442f-9f68-c47c6e7d09d4", Deleted = isDeleted };

        IAccessControlProvider<TableData> accessProvider = FakeAccessControlProvider<TableData>(TableOperation.Query, true);
        IRepository<TableData> repository = FakeRepository(entity, true);
        TableControllerOptions options = new() { EnableSoftDelete = true };
        TableController<TableData> controller = new(repository, accessProvider) { Options = options };
        controller.ControllerContext.HttpContext = CreateHttpContext(HttpMethod.Get, "https://localhost/table?__includedeleted=true");

        OkObjectResult result = await controller.QueryAsync() as OkObjectResult;
        result.Should().NotBeNull();
        PagedResult pagedResult = result.Value as PagedResult;
        pagedResult.Should().NotBeNull();
        pagedResult.Items.Should().HaveCount(count);
    }
    #endregion
}
