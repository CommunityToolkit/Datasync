// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Server.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using NSubstitute;

namespace CommunityToolkit.Datasync.Server.Test.Filters;

[ExcludeFromCodeCoverage]
public class DatasyncControllerAttribute_Tests
{
    private readonly ActionContext actionContext = new() { HttpContext = new DefaultHttpContext(), RouteData = new(), ActionDescriptor = new ActionDescriptor() };
    private readonly List<IFilterMetadata> filters = [];

    [Fact]
    public void OnResultExecuting_WithObject_UpdatesHeaders()
    {
        IActionResult result = new OkObjectResult(new TableData() { Version = [0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68], UpdatedAt = DateTimeOffset.Parse("2023-11-13T13:30:05.1234Z") });

        ResultExecutingContext context = new(this.actionContext, this.filters, result, Substitute.For<Controller>());
        DatasyncControllerAttribute attribute = new();

        attribute.OnResultExecuting(context);

        context.HttpContext.Response.Headers.Should().ContainKey("ETag").WhoseValue.Should().ContainSingle(v => v == "\"YWJjZGVmZ2g=\"");
        context.HttpContext.Response.Headers.Should().ContainKey("Last-Modified").WhoseValue.Should().ContainSingle(v => v == "Mon, 13 Nov 2023 13:30:05 GMT");
    }

    [Fact]
    public void OnResultExecuting_NotITableData_Works()
    {
        IActionResult result = new OkObjectResult("This is a test");

        ResultExecutingContext context = new(this.actionContext, this.filters, result, Substitute.For<Controller>());
        DatasyncControllerAttribute attribute = new();

        attribute.OnResultExecuting(context);

        context.HttpContext.Response.Headers.Should().BeEmpty();
    }

    [Fact]
    public void OnResultExecuting_NoContent_Works()
    {
        IActionResult result = new NoContentResult();

        ResultExecutingContext context = new(this.actionContext, this.filters, result, Substitute.For<Controller>());
        DatasyncControllerAttribute attribute = new();

        attribute.OnResultExecuting(context);

        context.HttpContext.Response.Headers.Should().BeEmpty();
    }

    [Theory]
    [InlineData(304, false)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    [InlineData(404, false)]
    [InlineData(409, true)]
    [InlineData(412, true)]
    public void OnException_WithHttpException_Works(int statusCode, bool hasPayload)
    {
        TableData entity = new() { Version = [0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68], UpdatedAt = DateTimeOffset.Parse("2023-11-13T13:30:05.1234Z") };
        HttpException exception = new(statusCode);
        if (hasPayload)
        {
            exception.Payload = entity;
        }

        ExceptionContext context = new(this.actionContext, this.filters) { Exception = exception };
        DatasyncControllerAttribute attribute = new();

        attribute.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        if (hasPayload)
        {
            context.Result.Should().BeOfType<JsonResult>().Which.StatusCode.Should().Be(statusCode);
            context.Result.Should().BeOfType<JsonResult>().Which.Value.Should().BeOfType<TableData>().And.BeEquivalentTo(entity);
            context.HttpContext.Response.Headers.Should().ContainKey("ETag").WhoseValue.Should().ContainSingle(v => v == "\"YWJjZGVmZ2g=\"");
            context.HttpContext.Response.Headers.Should().ContainKey("Last-Modified").WhoseValue.Should().ContainSingle(v => v == "Mon, 13 Nov 2023 13:30:05 GMT");
        }
        else
        {
            context.Result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(statusCode);
            context.HttpContext.Response.Headers.Should().BeEmpty();
        }
    }

    [Fact]
    public void OnException_DoesNotProcessOtherExceptions()
    {
        ExceptionContext context = new(this.actionContext, this.filters) { Exception = new ApplicationException() };
        DatasyncControllerAttribute attribute = new();

        attribute.OnException(context);

        context.ExceptionHandled.Should().BeFalse();
        context.HttpContext.Response.Headers.Should().BeEmpty();
    }

    [Fact]
    public void OnException_WithHttpException_WithoutITableData_DoesntSetHeaders()
    {
        const string entity = "foo";
        HttpException exception = new(400) { Payload = entity };

        ExceptionContext context = new(this.actionContext, this.filters) { Exception = exception };
        DatasyncControllerAttribute attribute = new();

        attribute.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        context.Result.Should().BeOfType<JsonResult>().Which.StatusCode.Should().Be(400);
        context.Result.Should().BeOfType<JsonResult>().Which.Value.Should().BeOfType<string>().And.BeEquivalentTo(entity);
        context.HttpContext.Response.Headers.Should().BeEmpty();
    }

    [Fact]
    public void OnException_ExceptionHandled_DoesntDoAnything()
    {
        this.actionContext.HttpContext.Response.Headers.ETag = new StringValues("\"foo\"");
        TableData entity = new() { Version = [0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68], UpdatedAt = DateTimeOffset.Parse("2023-11-13T13:30:05.1234Z") };
        HttpException exception = new(400) { Payload = entity };
        ExceptionContext context = new(this.actionContext, this.filters)
        {
            Exception = exception,
            ExceptionHandled = true,
            Result = new StatusCodeResult(200)
        };
        DatasyncControllerAttribute attribute = new();

        attribute.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        context.Result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(200);
        context.HttpContext.Response.Headers.Should().ContainKey("ETag").WhoseValue.Should().ContainSingle(v => v == "\"foo\"");
    }
}
