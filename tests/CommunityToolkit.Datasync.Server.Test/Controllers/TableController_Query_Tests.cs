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
    public void CatchClientSideEvaluationException_NotCCEE_ThrowsOriginalException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception");

        static void evaluator() { throw new ApplicationException("In evaluator"); }

        Action act = () => controller.CatchClientSideEvaluationException(exception, "foo", evaluator);
        act.Should().Throw<ApplicationException>().WithMessage("Original exception");
    }

    [Fact]
    public void CatchClientSideEvaluationException_NotCCEE_WithInner_ThrowsOriginalException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception", new ApplicationException());

        static void evaluator() { throw new ApplicationException("In evaluator"); }

        Action act = () => controller.CatchClientSideEvaluationException(exception, "foo", evaluator);
        act.Should().Throw<ApplicationException>().WithMessage("Original exception");
    }

    [Fact]
    public void CatchClientSideEvaluationException_CCEE_ThrowsEvaluatorException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        NotSupportedException exception = new("Original exception", new ApplicationException("foo"));

        static void evaluator() { throw new ApplicationException("In evaluator"); }

        Action act = () => controller.CatchClientSideEvaluationException(exception, "foo", evaluator);
        act.Should().Throw<ApplicationException>().WithMessage("In evaluator");
    }

    [Fact]
    public void CatchClientSideEvaluationException_CCEEInner_ThrowsEvaluatorException()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception", new NotSupportedException("foo"));

        static void evaluator() { throw new ApplicationException("In evaluator"); }

        Action act = () => controller.CatchClientSideEvaluationException(exception, "foo", evaluator);
        act.Should().Throw<ApplicationException>().WithMessage("In evaluator");
    }

    [Fact]
    public void CatchClientSideEvaluationException_CCEE_ExecutesEvaluator()
    {
        bool isExecuted = false;
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        NotSupportedException exception = new("Original exception", new ApplicationException("foo"));
        Action act = () => controller.CatchClientSideEvaluationException(exception, "foo", () => isExecuted = true);
        act.Should().NotThrow();
        isExecuted.Should().BeTrue();
    }

    [Fact]
    public void CatchClientSideEvaluationException_CCEEInner_ExecutesEvaluator()
    {
        bool isExecuted = false;
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        ApplicationException exception = new("Original exception", new NotSupportedException("foo"));
        Action act = () => controller.CatchClientSideEvaluationException(exception, "foo", () => isExecuted = true);
        act.Should().NotThrow();
        isExecuted.Should().BeTrue();
    }
    #endregion

    #region ExecuteQueryWithClientEvaluation
    [Fact]
    public void ExecuteQueryWithClientEvaluation_ExecutesServiceSide()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = true;

        int evaluations = 0;
        void evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            // if (evaluations == 1) throw new NotSupportedException("Server side");
            // if (evaluations == 2) throw new NotSupportedException("Client side");
        }

        List<InMemoryMovie> dataset = [];

        Action act = () => controller.ExecuteQueryWithClientEvaluation(dataset.AsQueryable(), evaluator);

        act.Should().NotThrow();
        evaluations.Should().Be(1);
    }

    [Fact]
    public void ExecuteQueryWithClientEvaluation_ThrowsServiceSide_WhenClientEvaluationDisabled()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = true;

        int evaluations = 0;
#pragma warning disable IDE0011 // Add braces
        void evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            if (evaluations == 1) throw new NotSupportedException("Server side");
            if (evaluations == 2) throw new NotSupportedException("Client side");
        }
#pragma warning restore IDE0011 // Add braces

        List<InMemoryMovie> dataset = [];

        Action act = () => controller.ExecuteQueryWithClientEvaluation(dataset.AsQueryable(), evaluator);

        act.Should().Throw<NotSupportedException>().WithMessage("Server side");
    }

    [Fact]
    public void ExecuteQueryWithClientEvaluation_ExecutesClientSide_WhenClientEvaluationEnabled()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = false;

        int evaluations = 0;
#pragma warning disable IDE0011 // Add braces
        void evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            if (evaluations == 1) throw new NotSupportedException("Server side");
            //if (evaluations == 2) throw new NotSupportedException("Client side");
        }
#pragma warning restore IDE0011 // Add braces

        List<InMemoryMovie> dataset = [];

        Action act = () => controller.ExecuteQueryWithClientEvaluation(dataset.AsQueryable(), evaluator);

        act.Should().NotThrow();
        evaluations.Should().Be(2);
    }

    [Fact]
    public void ExecuteQueryWithClientEvaluation_ThrowsClientSide_WhenClientEvaluationEnabled()
    {
        TableController<InMemoryMovie> controller = new() { Repository = new InMemoryRepository<InMemoryMovie>() };
        controller.Options.DisableClientSideEvaluation = false;

        int evaluations = 0;
#pragma warning disable IDE0011 // Add braces
        void evaluator(IQueryable<InMemoryMovie> dataset)
        {
            evaluations++;
            if (evaluations == 1) throw new NotSupportedException("Server side", new ApplicationException("Inner exception"));
            if (evaluations == 2) throw new NotSupportedException("Client side");
        }
#pragma warning restore IDE0011 // Add braces

        List<InMemoryMovie> dataset = [];

        Action act = () => controller.ExecuteQueryWithClientEvaluation(dataset.AsQueryable(), evaluator);

        act.Should().Throw<NotSupportedException>().WithMessage("Client side");
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
