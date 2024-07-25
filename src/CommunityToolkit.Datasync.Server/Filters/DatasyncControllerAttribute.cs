// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Datasync.Server.Filters;

/// <summary>
/// Modifies the response from a datasync controller to ensure the appropriate protocol
/// between a datasync client and server is followed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class DatasyncControllerAttribute : ResultFilterAttribute, IExceptionFilter
{
    /// <inheritdoc/>
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult result)
        {
            if (result.Value is ITableData entity)
            {
                context.HttpContext.Response.Headers.SetConditionalHeaders(entity);
                if (result.StatusCode == StatusCodes.Status201Created)
                {
                    context.HttpContext.Response.Headers.Location = $"{context.HttpContext.Request.GetDisplayUrl()}/{entity.Id}";
                }
            }

            IDatasyncServiceOptions options = GetDatasyncServiceOptions(context.HttpContext);
            context.Result = new JsonResult(result.Value, options.JsonSerializerOptions) { StatusCode = result.StatusCode };
        }

        base.OnResultExecuting(context);
    }

    /// <inheritdoc/>
    public void OnException(ExceptionContext context)
    {
        if (!context.ExceptionHandled && context.Exception is HttpException exception)
        {
            IDatasyncServiceOptions options = GetDatasyncServiceOptions(context.HttpContext);
            context.Result = exception.Payload is null
                ? new StatusCodeResult(exception.StatusCode)
                : new JsonResult(exception.Payload, options.JsonSerializerOptions) { StatusCode = exception.StatusCode };

            if (exception.Payload is ITableData entity)
            {
                context.HttpContext.Response.Headers.SetConditionalHeaders(entity);
            }

            context.ExceptionHandled = true;
        }
    }

    /// <summary>
    /// Retrieves the <see cref="IDatasyncServiceOptions"/> from the request services.
    /// </summary>
    /// <param name="context">The context to use to retrieve the settings.</param>
    /// <returns></returns>
    private static IDatasyncServiceOptions GetDatasyncServiceOptions(HttpContext context)
        => context.RequestServices?.GetService<IDatasyncServiceOptions>() ?? new DatasyncServiceOptions();
}
