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

        IQueryable<TEntity> dataset = (await Repository.AsQueryableAsync(cancellationToken).ConfigureAwait(false))
            .ApplyDataView(AccessControlProvider.GetDataView())
            .ApplyDeletedView(Request, Options.EnableSoftDelete);

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

        // Note that some IQueryable providers cannot execute all queries against the data source, so we have
        // to switch to in-memory processing for those queries.  This is done by calling ToListAsync() on the
        // IQueryable.  This is not ideal, but it is the only way to support all of the OData query options.
        IEnumerable<object>? results = null;
        ExecuteQueryWithClientEvaluation(dataset, ds => results = (IEnumerable<object>)queryOptions.ApplyTo(ds, querySettings));

        int count = 0;
        FilterQueryOption? filter = queryOptions.Filter;
        ExecuteQueryWithClientEvaluation(dataset, ds => { IQueryable<TEntity> q = (IQueryable<TEntity>)(filter?.ApplyTo(ds, new ODataQuerySettings()) ?? ds); count = q.Count(); });

        PagedResult result = BuildPagedResult(queryOptions, results, count);
        Logger.LogInformation("Query: {Count} items being returned", result.Items.Count());
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
            .AddScoped<ODataQuerySettings>()
            .AddSingleton<ODataUriResolver>(_ => new UnqualifiedODataUriResolver { EnableCaseInsensitive = true })
            .AddScoped<ODataSimplifiedOptions>()
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
        if (results is IEnumerable<ISelectExpandWrapper> wrapper)
        {
            results = wrapper.Select(x => x.ToDictionary());
        }

        PagedResult result = new(results ?? []) { Count = queryOptions.Count != null ? count : null };
        if (queryOptions.Top != null)
        {
            result.NextLink = skip >= count || top <= 0 ? null : Request.CreateNextLink(skip, top);
        }
        else
        {
            result.NextLink = skip >= count ? null : Request.CreateNextLink(skip, 0);
        }

        return result;
    }

    /// <summary>
    /// When doing a query evaluation, certain providers (e.g. Entity Framework) require some things
    /// to be done client side.  We use a client side evaluator to handle this case when it happens.
    /// </summary>
    /// <param name="ex">The exception thrown by the service-side evaluator</param>
    /// <param name="reason">The reason if the client-side evaluator throws.</param>
    /// <param name="clientSideEvaluator">The client-side evaluator</param>
    [NonAction]
    internal void CatchClientSideEvaluationException(Exception ex, string reason, Action clientSideEvaluator)
    {
        if (IsClientSideEvaluationException(ex) || IsClientSideEvaluationException(ex.InnerException))
        {
            try
            {
                clientSideEvaluator.Invoke();
            }
            catch (Exception err)
            {
                Logger.LogError("Error while {reason}: {Message}", reason, err.Message);
                throw;
            }
        }
        else
        {
            throw ex;
        }
    }

    /// <summary>
    /// Executes an evaluation of a query, using a client-side evaluation if necessary.
    /// </summary>
    /// <param name="dataset">The dataset to be evaluated.</param>
    /// <param name="evaluator">The base evaluation to be performed.</param>
    [NonAction]
    internal void ExecuteQueryWithClientEvaluation(IQueryable<TEntity> dataset, Action<IQueryable<TEntity>> evaluator)
    {
        try
        {
            evaluator.Invoke(dataset);
        }
        catch (Exception ex) when (!Options.DisableClientSideEvaluation)
        {
            CatchClientSideEvaluationException(ex, "executing query", () =>
            {
                Logger.LogWarning("Error while executing query: possible client-side evaluation ({Message})", ex.InnerException?.Message ?? ex.Message);
                evaluator.Invoke(dataset.ToList().AsQueryable());
            });
        }
    }

    /// <summary>
    /// Determines if a particular exception indicates a client-side evaluation is required.
    /// </summary>
    /// <param name="ex">The exception that was thrown by the service-side evaluator</param>
    /// <returns>true if a client-side evaluation is required.</returns>
    [NonAction]
    [SuppressMessage("Roslynator", "RCS1158:Static member in generic type should use a type parameter.")]
    internal static bool IsClientSideEvaluationException(Exception? ex)
        => ex is not null and (InvalidOperationException or NotSupportedException);
}
