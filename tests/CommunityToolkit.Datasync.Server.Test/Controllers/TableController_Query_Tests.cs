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

    #region CatchClientSideEvaluationException
    [Fact]
    public async Task CatchClientSideEvaluationException_NotCCEE_ThrowsOriginalException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception");

        static Task evaluator() { throw new ApplicationException("In evaluator"); }

        Func<Task> act = async () => await controller.CatchClientSideEvaluationExceptionAsync(exception, "foo", evaluator);
        (await act.Should().ThrowAsync<ApplicationException>()).WithMessage("Original exception");
    }

    [Fact]
    public async Task CatchClientSideEvaluationException_NotCCEE_WithInner_ThrowsOriginalException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception", new ApplicationException());

        static Task evaluator() { throw new ApplicationException("In evaluator"); }

        Func<Task> act = async () => await controller.CatchClientSideEvaluationExceptionAsync(exception, "foo", evaluator);
        (await act.Should().ThrowAsync<ApplicationException>()).WithMessage("Original exception");
    }

    [Fact]
    public async Task CatchClientSideEvaluationException_CCEE_ThrowsEvaluatorException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        NotSupportedException exception = new("Original exception", new ApplicationException("foo"));

        static Task evaluator() { throw new ApplicationException("In evaluator"); }

        Func<Task> act = async () => await controller.CatchClientSideEvaluationExceptionAsync(exception, "foo", evaluator);
        (await act.Should().ThrowAsync<ApplicationException>()).WithMessage("In evaluator");
    }

    [Fact]
    public async Task CatchClientSideEvaluationException_CCEEInner_ThrowsEvaluatorException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception", new NotSupportedException("foo"));

        static Task evaluator() { throw new ApplicationException("In evaluator"); }

        Func<Task> act = async () => await controller.CatchClientSideEvaluationExceptionAsync(exception, "foo", evaluator);
        (await act.Should().ThrowAsync<ApplicationException>()).WithMessage("In evaluator");
    }

    [Fact]
    public async Task CatchClientSideEvaluationException_CCEE_ExecutesEvaluator()
    {
        bool isExecuted = false;
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        NotSupportedException exception = new("Original exception", new ApplicationException("foo"));

        Func<Task> act = async () => await controller.CatchClientSideEvaluationExceptionAsync(exception, "foo", () => { isExecuted = true; return Task.CompletedTask; });
        await act.Should().NotThrowAsync();
        isExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task CatchClientSideEvaluationException_CCEEInner_ExecutesEvaluator()
    {
        bool isExecuted = false;
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception", new NotSupportedException("foo"));

        Func<Task> act = async () => await controller.CatchClientSideEvaluationExceptionAsync(exception, "foo", () => { isExecuted = true; return Task.CompletedTask; });
        await act.Should().NotThrowAsync();
        isExecuted.Should().BeTrue();
    }
    #endregion

    #region ExecuteQueryWithClientEvaluation
    [Fact]
    public async Task ExecuteQueryWithClientEvaluation_ExecutesServiceSide()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = true;

        int evaluations = 0;
        Task evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            // if (evaluations == 1) throw new NotSupportedException("Server side");
            // if (evaluations == 2) throw new NotSupportedException("Client side");
            return Task.CompletedTask;
        }

        List<InMemoryMovie> dataset = [];

        Func<Task> act = async () => await controller.ExecuteQueryWithClientEvaluationAsync(dataset.AsQueryable(), evaluator);

        await act.Should().NotThrowAsync();
        evaluations.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteQueryWithClientEvaluation_ThrowsServiceSide_WhenClientEvaluationDisabled()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = true;

        int evaluations = 0;
#pragma warning disable IDE0011 // Add braces
        Task evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            if (evaluations == 1) throw new NotSupportedException("Server side");
            if (evaluations == 2) throw new NotSupportedException("Client side");
            return Task.CompletedTask;
        }
#pragma warning restore IDE0011 // Add braces

        List<InMemoryMovie> dataset = [];

        Func<Task> act = async () => await controller.ExecuteQueryWithClientEvaluationAsync(dataset.AsQueryable(), evaluator);

        (await act.Should().ThrowAsync<NotSupportedException>()).WithMessage("Server side");
    }

    [Fact]
    public async Task ExecuteQueryWithClientEvaluation_ExecutesClientSide_WhenClientEvaluationEnabled()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = false;

        int evaluations = 0;
#pragma warning disable IDE0011 // Add braces
        Task evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            if (evaluations == 1) throw new NotSupportedException("Server side");
            //if (evaluations == 2) throw new NotSupportedException("Client side");
            return Task.CompletedTask;
        }
#pragma warning restore IDE0011 // Add braces

        List<InMemoryMovie> dataset = [];

        Func<Task> act = async () => await controller.ExecuteQueryWithClientEvaluationAsync(dataset.AsQueryable(), evaluator);

        await act.Should().NotThrowAsync();
        evaluations.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteQueryWithClientEvaluation_ThrowsClientSide_WhenClientEvaluationEnabled()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = false;

        int evaluations = 0;
#pragma warning disable IDE0011 // Add braces
        Task evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            if (evaluations == 1) throw new NotSupportedException("Server side", new ApplicationException("Inner exception"));
            if (evaluations == 2) throw new NotSupportedException("Client side");
            return Task.CompletedTask;
        }
#pragma warning restore IDE0011 // Add braces

        List<InMemoryMovie> dataset = [];

        Func<Task> act = async () => await controller.ExecuteQueryWithClientEvaluationAsync(dataset.AsQueryable(), evaluator);

        (await act.Should().ThrowAsync<NotSupportedException>()).WithMessage("Client side");
        evaluations.Should().Be(2);
    }
    #endregion

    [Fact]
    public void IsClientSideEvaluationException_Works()
    {
        Assert.False(TableController<InMemoryMovie>.IsClientSideEvaluationException(null));
        Assert.True(TableController<InMemoryMovie>.IsClientSideEvaluationException(new InvalidOperationException()));
        Assert.True(TableController<InMemoryMovie>.IsClientSideEvaluationException(new NotSupportedException()));
        Assert.False(TableController<InMemoryMovie>.IsClientSideEvaluationException(new ApplicationException()));
    }

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
