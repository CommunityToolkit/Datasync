// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Query.Wrapper;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OData.UriParser;
using Microsoft.OData;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Datasync.Server.OData;

namespace CommunityToolkit.Datasync.Server;

public partial class TableController<TEntity> : ODataController where TEntity : class, ITableData
{
    private const string SkipParameterName = "$skip";
    private const string TopParameterName = "$top";

    /// <summary>
    /// <para>
    /// The GET method is used to retrieve resource representation.  The resource is never modified.
    /// In this case, an OData v4 query is accepted with the following options:
    /// </para>
    /// <para>
    /// - <c>$count</c> is used to return a count of entities within the search parameters within the <see cref="PagedResult"/> response.
    /// - <c>$filter</c> is used to restrict the entities to be sent.
    /// - <c>$orderby</c> is used for ordering the entities to be sent.
    /// - <c>$select</c> is used to select which properties of the entities are sent.
    /// - <c>$skip</c> is used to skip some entities
    /// - <c>$top</c> is used to limit the number of entities returned.
    /// </para>
    /// <para>
    /// In addition, the <c>__includeDeleted</c> parameter is used to decide whether to include soft-deleted items in the result.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An <see cref="OkObjectResult"/> response object with the items.</returns>
    [HttpGet]
    [ActionName("QueryAsync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> QueryAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("QueryAsync: {querystring}", HttpContext.Request.QueryString);
        await AuthorizeRequestAsync(TableOperation.Query, null, cancellationToken).ConfigureAwait(false);
        _ = BuildServiceProvider(Request);

        ODataValidationSettings validationSettings = new() { MaxTop = Options.MaxTop };
        ODataQuerySettings querySettings = new() { PageSize = Options.PageSize, EnsureStableOrdering = true };
        ODataQueryContext queryContext = new(EdmModel, typeof(TEntity), new ODataPath());
        ODataQueryOptions<TEntity> queryOptions = new(queryContext, Request);

        try
        {
            queryOptions.Validate(validationSettings);
        }
        catch (ODataException validationException)
        {
            Logger.LogWarning("Query: Error when validating query: {Message}", validationException.Message);
            return BadRequest(validationException.Message);
        }

        // Determine the dataset to be queried for this user.
        IQueryable<TEntity> dataset = (await Repository.AsQueryableAsync(cancellationToken).ConfigureAwait(false))
            .ApplyDataView(AccessControlProvider.GetDataView())
            .ApplyDeletedView(Request, Options.EnableSoftDelete);

        // Apply the requested filter from the OData transaction.
        IQueryable<TEntity> filteredDataset = dataset.ApplyODataFilter(queryOptions.Filter, querySettings);

        // Count the number of items within the filtered dataset - this is used when $count is requested.
        int filteredCount;
        try
        {
            filteredCount = await Repository.CountAsync(filteredDataset, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            throw new HttpException(400, "Client-side evaluation is not supported. Please ensure that the query can be translated to a server-side query.");
        }

        // Now apply the OrderBy, Skip, and Top options to the dataset.
        IQueryable<TEntity> orderedDataset = filteredDataset
            .ApplyODataOrderBy(queryOptions.OrderBy, querySettings)
            .ApplyODataPaging(queryOptions, querySettings);

        // Get the list of items within the dataset that need to be returned.
        IList<TEntity> entitiesInResultSet;
        try
        {
            entitiesInResultSet = await Repository.ToListAsync(orderedDataset, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            throw new HttpException(400, "Client-side evaluation is not supported. Please ensure that the query can be translated to a server-side query.");
        }

        // Produce the paged result.
        PagedResult result = BuildPagedResult(queryOptions, entitiesInResultSet.ApplyODataSelect(queryOptions.SelectExpand, querySettings), filteredCount);
        Logger.LogInformation("Query: {Count} items being returned", entitiesInResultSet.Count);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new <see cref="IServiceProvider"/> for the request to handle OData requests.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> currently being processed.</param>
    /// <returns>An <see cref="IServiceProvider"/> for the request pipeline.</returns>
    [NonAction]
    [SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Lots of service adjustments where the result is never used.")]
    protected static IServiceProvider BuildServiceProvider(HttpRequest request)
    {
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton(_ => new DefaultQueryConfigurations
        {
            EnableCount = true,
            EnableFilter = true,
            EnableOrderBy = true,
            EnableSelect = true
        });

        services
            .AddScoped<IFilterBinder, DatasyncFilterBinder>()
            .AddScoped<ODataQuerySettings>(_ => new ODataQuerySettings { EnsureStableOrdering = true })
            .AddSingleton<ODataUriResolver>(_ => new UnqualifiedODataUriResolver { EnableCaseInsensitive = true })
            .AddScoped<ODataUriParserSettings>();

        IServiceProvider provider = services.BuildServiceProvider();
        request.ODataFeature().Services = provider;
        return provider;
    }

    /// <summary>
    /// Creates a <see cref="PagedResult"/> object from the results of a query.
    /// </summary>
    /// <param name="queryOptions">The OData query options to use in constructing the result.</param>
    /// <param name="results">The set of results.</param>
    /// <param name="count">The count of items to be returned by the query without paging.</param>
    /// <returns>A <see cref="PagedResult"/> object.</returns>
    [NonAction]
    internal PagedResult BuildPagedResult(ODataQueryOptions queryOptions, IEnumerable<object>? results, int count)
    {
        int resultCount = results?.Count() ?? 0;
        int skip = (queryOptions.Skip?.Value ?? 0) + resultCount;
        int top = (queryOptions.Top?.Value ?? 0) - resultCount;

        // Internal function to create the nextLink property for the paged result.
        static string CreateNextLink(HttpRequest request, int skip = 0, int top = 0)
        {
            string? queryString = request.QueryString.Value;
            List<string> query = (queryString ?? "").TrimStart('?')
                .Split('&')
                .Where(q => !q.StartsWith($"{SkipParameterName}=") && !q.StartsWith($"{TopParameterName}="))
                .ToList();

            if (skip > 0)
            {
                query.Add($"{SkipParameterName}={skip}");
            }

            if (top > 0)
            {
                query.Add($"{TopParameterName}={top}");
            }

            return string.Join('&', query).TrimStart('&');
        }

        PagedResult result = new(results ?? []) { Count = queryOptions.Count != null ? count : null };
        if (queryOptions.Top is not null)
        {
            result.NextLink = skip >= count || top <= 0 ? null : CreateNextLink(Request, skip, top);
        }
        else
        {
            result.NextLink = skip >= count ? null : CreateNextLink(Request, skip, 0);
        }

        return result;
    }
}
