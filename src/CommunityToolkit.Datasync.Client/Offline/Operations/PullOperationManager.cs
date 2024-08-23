// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Exceptions;
using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Threading;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline.Operations;

/// <summary>
/// The internals of a single pull operation.
/// </summary>
/// <remarks>
/// The synchronization lock is already assumed to be in place.  The internals do not do
/// interim saves of data.
/// </remarks>
/// <param name="context">The <see cref="OfflineDbContext"/> to use for saving data</param>
/// <param name="synchronizableTypes">The list of synchronizable types that the pull operation should support.</param>
internal class PullOperationManager(OfflineDbContext context, IEnumerable<Type> synchronizableTypes) : IPullOperationManager
{
    /// <summary>
    /// The delta-token store, which stores the date/time of the last synchronization for a query.
    /// </summary>
    internal IDeltaTokenStore DeltaTokenStore { get => context.DeltaTokenStore; }

    /// <summary>
    /// The list of synchronizable entity types that this object can process.
    /// </summary>
    internal IList<Type> SynchronizableTypes { get; } = [.. synchronizableTypes];

    /// <summary>
    /// Executes a set of pull requests.
    /// </summary>
    /// <param name="requests">The list of pull requests to execute..</param>
    /// <param name="pullOptions">The pull options to use.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    public async Task<PullResult> ExecuteAsync(IEnumerable<PullRequest> requests, PullOptions pullOptions, CancellationToken cancellationToken = default)
    {
        ArgumentValidationException.ThrowIfNotValid(pullOptions, nameof(pullOptions));
        PullResult result = new();

        QueueHandler<PullResponse> databaseUpdateQueue = new(1, async pullResponse =>
        {
            DateTimeOffset lastSynchronization = await DeltaTokenStore.GetDeltaTokenAsync(pullResponse.QueryId, cancellationToken).ConfigureAwait(false);
            foreach (object item in pullResponse.Items)
            {
                EntityMetadata metadata = EntityResolver.GetEntityMetadata(item, pullResponse.EntityType);
                object? originalEntity = await context.FindAsync(pullResponse.EntityType, [metadata.Id], cancellationToken).ConfigureAwait(false);

                if (originalEntity is null && !metadata.Deleted)
                {
                    _ = context.Add(item);
                    result.IncrementAdditions();
                } 
                else if (originalEntity is not null && metadata.Deleted)
                {
                    _ = context.Remove(originalEntity);
                    result.IncrementDeletions();
                } 
                else if (originalEntity is not null && !metadata.Deleted)
                {
                    context.Entry(originalEntity).CurrentValues.SetValues(item);
                    result.IncrementReplacements();
                }

                if (metadata.UpdatedAt.HasValue && metadata.UpdatedAt.Value > lastSynchronization)
                {
                    await DeltaTokenStore.SetDeltaTokenAsync(pullResponse.QueryId, metadata.UpdatedAt.Value, cancellationToken).ConfigureAwait(false);
                }
            }

            if (pullOptions.SaveAfterEveryServiceRequest)
            {
                _ = await context.SaveChangesAsync(true, false, cancellationToken).ConfigureAwait(false);
            }
        });

        QueueHandler<PullRequest> serviceRequestQueue = new(pullOptions.ParallelOperations, async pullRequest =>
        {
            Uri endpoint = ExecutableOperation.MakeAbsoluteUri(pullRequest.HttpClient.BaseAddress, pullRequest.Endpoint);
            Uri requestUri = new UriBuilder(endpoint) { Query = pullRequest.QueryDescription.ToODataQueryString() }.Uri;
            Type pageType = typeof(Page<>).MakeGenericType(pullRequest.EntityType);

            try
            {
                bool completed = false;
                do
                {
                    Page<object> page = await GetPageAsync(pullRequest.HttpClient, requestUri, pageType, cancellationToken).ConfigureAwait(false);
                    databaseUpdateQueue.Enqueue(new PullResponse(pullRequest.EntityType, pullRequest.QueryId, page.Items));
                    if (!string.IsNullOrEmpty(page.NextLink))
                    {
                        requestUri = new UriBuilder(endpoint) { Query = page.NextLink }.Uri;
                    }
                    else
                    {
                        completed = true;
                    }
                }
                while (!completed);
            }
            catch (DatasyncPullException ex)
            {
                result.AddFailedRequest(requestUri, ex.ServiceResponse);
            }
        });

        foreach (PullRequest request in requests)
        {
            request.QueryId = !string.IsNullOrEmpty(request.QueryId) ? request.QueryId : PullRequestBuilder.GetQueryIdFromQuery(request.EntityType, request.QueryDescription);
            DateTimeOffset lastSynchronization = await context.DeltaTokenStore.GetDeltaTokenAsync(request.QueryId, cancellationToken).ConfigureAwait(false);
            PrepareQueryDescription(request.QueryDescription, lastSynchronization);
            serviceRequestQueue.Enqueue(request);
        }

        await serviceRequestQueue.WhenComplete();
        await databaseUpdateQueue.WhenComplete();
        return result;
    }

    /// <summary>
    /// Retrieves a single page of results from the service.  This throws an error if the response is invalid, and decodes the page of results.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="pageType">The page type (actual return type from the service).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A page of items.</returns>
    /// <exception cref="DatasyncPullException">Thrown on error</exception>
    internal async Task<Page<object>> GetPageAsync(HttpClient client, Uri requestUri, Type pageType, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUri);
        using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        ServiceResponse response = new(responseMessage);
        if (!response.IsSuccessful || !response.HasContent)
        {
            throw new DatasyncPullException() { ServiceResponse = response };
        }

        try
        {
            object? result = await JsonSerializer.DeserializeAsync(response.ContentStream, pageType, context.JsonSerializerOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new DatasyncPullException("JSON result is null") { ServiceResponse = response };
            return (Page<object>)result;
        }
        catch (JsonException ex)
        {
            throw new DatasyncPullException(ex.Message, ex) { ServiceResponse = response };
        }
    }

    /// <summary>
    /// Prepares the query description for use as a pull request.
    /// </summary>
    /// <param name="query">The query description to modify.</param>
    /// <param name="lastSynchronization">The last synchronization date/time</param>
    internal static void PrepareQueryDescription(QueryDescription query, DateTimeOffset lastSynchronization)
    {
        if (lastSynchronization.ToUnixTimeMilliseconds() > 0L)
        {
            BinaryOperatorNode deltaTokenFilter = new(BinaryOperatorKind.GreaterThan)
            {
                LeftOperand = new MemberAccessNode(null, "updatedAt"),
                RightOperand = new ConstantNode(lastSynchronization)
            };
            query.Filter = query.Filter is null ? deltaTokenFilter : new BinaryOperatorNode(BinaryOperatorKind.And, query.Filter, deltaTokenFilter);
        }

        query.QueryParameters.Add(ODataQueryParameters.IncludeDeleted, "true");
        query.RequestTotalCount = true;
        query.Top = null;
        query.Skip = 0;
        query.Ordering.Clear();
        query.Ordering.Add(new OrderByNode(new MemberAccessNode(null, "updatedAt"), true));
    }

    /// <summary>
    /// A record type for the database update queue handler.
    /// </summary>
    /// <param name="EntityType">The type of entity contained within the items.</param>
    /// <param name="QueryId">The query ID for the request.</param>
    /// <param name="Items">The list of items to process.</param>
    internal record PullResponse(Type EntityType, string QueryId, IEnumerable<object> Items);
}
