// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Exceptions;
using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Threading;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static CommunityToolkit.Datasync.Client.Offline.Operations.PullOperationManager;

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
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "Readability")]
    public async Task<PullResult> ExecuteAsync(IEnumerable<PullRequest> requests, PullOptions pullOptions, CancellationToken cancellationToken = default)
    {
        ArgumentValidationException.ThrowIfNotValid(pullOptions, nameof(pullOptions));
        PullResult result = new();

        QueueHandler<PullResponse> databaseUpdateQueue = new(1, async pullResponse =>
        {
            EntityMetadata? currentMetadata = null;

            try
            {
                if (pullResponse.Items.Any())
                {
                    DateTimeOffset lastSynchronization = await DeltaTokenStore.GetDeltaTokenAsync(pullResponse.QueryId, cancellationToken).ConfigureAwait(false);
                    foreach (object item in pullResponse.Items)
                    {
                        EntityMetadata metadata = EntityResolver.GetEntityMetadata(item, pullResponse.EntityType);
                        currentMetadata = metadata;
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
                            // Gather properties marked with [JsonIgnore]
                            HashSet<string> ignoredProps = pullResponse.EntityType
                                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(p => p.IsDefined(typeof(JsonIgnoreAttribute), inherit: true))
                                .Select(p => p.Name)
                                .ToHashSet();

                            EntityEntry originalEntry = context.Entry(originalEntity);
                            EntityEntry newEntry = context.Entry(item);

                            // Only copy properties that are not marked with [JsonIgnore]
                            foreach (IProperty property in originalEntry.Metadata.GetProperties())
                            {
                                if (!ignoredProps.Contains(property.Name))
                                {
                                    originalEntry.Property(property.Name).CurrentValue = newEntry.Property(property.Name).CurrentValue;
                                }
                            }

                            result.IncrementReplacements();
                        }

                        if (metadata.UpdatedAt > lastSynchronization)
                        {
                            lastSynchronization = metadata.UpdatedAt.Value;
                            bool isAdded = await DeltaTokenStore.SetDeltaTokenAsync(pullResponse.QueryId, metadata.UpdatedAt.Value, cancellationToken).ConfigureAwait(false);
                            if (isAdded)
                            {
                                // Sqlite oddity - you can't add then update; it changes the change type to UPDATE, which then fails.
                                _ = await context.SaveChangesAsync(true, false, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        currentMetadata = null;
                    }

                    if (pullOptions.SaveAfterEveryServiceRequest)
                    {
                        _ = await context.SaveChangesAsync(true, false, cancellationToken).ConfigureAwait(false);
                    }

                    context.SendSynchronizationEvent(new SynchronizationEventArgs()
                    {
                        EventType = SynchronizationEventType.ItemsCommitted,
                        EntityType = pullResponse.EntityType,
                        ItemsProcessed = pullResponse.TotalItemsProcessed,
                        ItemsTotal = pullResponse.TotalRequestItems,
                        QueryId = pullResponse.QueryId
                    });
                }

                if (pullResponse.Completed)
                {
                    context.SendSynchronizationEvent(new SynchronizationEventArgs()
                    {
                        EventType = SynchronizationEventType.PullEnded,
                        EntityType = pullResponse.EntityType,
                        ItemsProcessed = pullResponse.TotalItemsProcessed,
                        ItemsTotal = pullResponse.TotalRequestItems,
                        QueryId = pullResponse.QueryId,
                        Exception = pullResponse.Exception,
                        ServiceResponse = pullResponse.Exception is DatasyncPullException ex ? ex.ServiceResponse : null
                    });
                }
            }
            catch (Exception ex)
            {
                // An exception is thrown in the local processing section of the pull operation.  We can't
                // handle it properly, so we add it to the result and send a synchronization event to allow
                // the developer to capture the exception.
                result.AddLocalException(currentMetadata, ex);
                context.SendSynchronizationEvent(new SynchronizationEventArgs()
                {
                    EventType = SynchronizationEventType.LocalException,
                    EntityType = pullResponse.EntityType,
                    QueryId = pullResponse.QueryId,
                    Exception = ex,
                    EntityMetadata = currentMetadata
                });
            }
        });

        QueueHandler<PullRequest> serviceRequestQueue = new(pullOptions.ParallelOperations, async pullRequest =>
        {
            Uri endpoint = ExecutableOperation.MakeAbsoluteUri(pullRequest.HttpClient.BaseAddress, pullRequest.Endpoint);
            Uri requestUri = new UriBuilder(endpoint) { Query = pullRequest.QueryDescription.ToODataQueryString() }.Uri;
            Type pageType = typeof(Page<>).MakeGenericType(pullRequest.EntityType);
            long itemsProcessed = 0;
            long totalCount = 0;

            try
            {
                bool completed = false;
                // Signal we started the pull operation.
                context.SendSynchronizationEvent(new SynchronizationEventArgs()
                {
                    EventType = SynchronizationEventType.PullStarted,
                    EntityType = pullRequest.EntityType,
                    QueryId = pullRequest.QueryId
                });
                do
                {
                    Page<object> page = await GetPageAsync(pullRequest.HttpClient, requestUri, pageType, cancellationToken).ConfigureAwait(false);
                    itemsProcessed += page.Items.Count();
                    totalCount = page.Count ?? totalCount;

                    context.SendSynchronizationEvent(new SynchronizationEventArgs()
                    {
                        EventType = SynchronizationEventType.ItemsFetched,
                        EntityType = pullRequest.EntityType,
                        ItemsProcessed = itemsProcessed,
                        ItemsTotal = page.Count ?? 0,
                        QueryId = pullRequest.QueryId
                    });

                    if (!string.IsNullOrEmpty(page.NextLink))
                    {
                        requestUri = new UriBuilder(endpoint) { Query = page.NextLink }.Uri;
                    }
                    else
                    {
                        completed = true;
                    }

                    databaseUpdateQueue.Enqueue(new PullResponse(pullRequest.EntityType, pullRequest.QueryId, page.Items, totalCount, itemsProcessed, completed));
                }
                while (!completed);
            }
            catch (DatasyncPullException ex)
            {
                result.AddFailedRequest(requestUri, ex.ServiceResponse);
                databaseUpdateQueue.Enqueue(new PullResponse(pullRequest.EntityType, pullRequest.QueryId, [], totalCount, itemsProcessed, true, ex));
            }
            catch (Exception localex)
            {
                // An exception is thrown that is locally generated.  We can't handle it properly, so we
                // add it to the result and send a synchronization event to allow the developer to capture
                // the exception.
                result.AddLocalException(null, localex);
                context.SendSynchronizationEvent(new SynchronizationEventArgs()
                {
                    EventType = SynchronizationEventType.LocalException,
                    EntityType = pullRequest.EntityType,
                    QueryId = pullRequest.QueryId,
                    Exception = localex
                });
            }
        });

        // Get requests we need to enqueue. Note : do not enqueue them yet. Context only supports one outstanding query at a time and we don't want a query from a background task being run concurrently with GetDeltaTokenAsync.
        List<PullRequest> requestsToEnqueue = [];
        foreach (PullRequest request in requests)
        {
            DateTimeOffset lastSynchronization = await context.DeltaTokenStore.GetDeltaTokenAsync(request.QueryId, cancellationToken).ConfigureAwait(false);
            request.QueryDescription = PrepareQueryDescription(request.QueryDescription, lastSynchronization);
            requestsToEnqueue.Add(request);
        }

        // Now enqueue the requests.
        foreach (PullRequest request in requestsToEnqueue)
        {
            serviceRequestQueue.Enqueue(request);
        }

        await serviceRequestQueue.WhenComplete();
        await databaseUpdateQueue.WhenComplete();
        _ = await context.SaveChangesAsync(true, false, cancellationToken).ConfigureAwait(false);
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
        PropertyInfo countPropInfo = pageType.GetProperty("Count")
            ?? throw new DatasyncException($"Page type '{pageType.Name}' does not have a 'Count' property");
        PropertyInfo itemsPropInfo = pageType.GetProperty("Items")
            ?? throw new DatasyncException($"Page type '{pageType.Name}' does not have an 'Items' property");
        PropertyInfo nextLinkPropInfo = pageType.GetProperty("NextLink")
            ?? throw new DatasyncException($"Page type '{pageType.Name}' does not have a 'NextLink' property");

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

            return new Page<object>()
            {
                Count = (long?)countPropInfo.GetValue(result),
                Items = (IEnumerable<object>)itemsPropInfo.GetValue(result)!,
                NextLink = (string?)nextLinkPropInfo.GetValue(result)
            };
        }
        catch (JsonException ex)
        {
            throw new DatasyncPullException(ex.Message, ex) { ServiceResponse = response };
        }
    }

    /// <summary>
    /// Prepares the query description for use as a pull request.
    /// </summary>
    /// <param name="source">The query description to modify.</param>
    /// <param name="lastSynchronization">The last synchronization date/time</param>
    /// <returns>A modified query description for the actual query.</returns>
    internal static QueryDescription PrepareQueryDescription(QueryDescription source, DateTimeOffset lastSynchronization)
    {
        QueryDescription query = new(source);
        if (lastSynchronization.ToUnixTimeMilliseconds() > 0L)
        {
            BinaryOperatorNode deltaTokenFilter = new(BinaryOperatorKind.GreaterThan)
            {
                LeftOperand = new MemberAccessNode(null, "updatedAt"),
                RightOperand = new ConstantNode(lastSynchronization)
            };
            query.Filter = query.Filter is null ? deltaTokenFilter : new BinaryOperatorNode(BinaryOperatorKind.And, query.Filter, deltaTokenFilter);
        }

        query.QueryParameters[ODataQueryParameters.IncludeDeleted] = "true";
        query.RequestTotalCount = true;
        query.Top = null;
        query.Skip = 0;
        query.Ordering.Clear();
        query.Ordering.Add(new OrderByNode(new MemberAccessNode(null, "updatedAt"), true));
        return query;
    }

    /// <summary>
    /// A record type for the database update queue handler.
    /// </summary>
    /// <param name="EntityType">The type of entity contained within the items.</param>
    /// <param name="QueryId">The query ID for the request.</param>
    /// <param name="Items">The list of items to process.</param>
    /// <param name="TotalRequestItems">The total number of items in the current pull request.</param>
    /// <param name="TotalItemsProcessed">The total number of items processed, <paramref name="Items"/> included.</param>
    /// <param name="Completed">If <c>true</c>, indicates that the pull request is completed.</param>
    /// <param name="Exception">Indicates an exception occured during fetching of data</param>
    [ExcludeFromCodeCoverage]
    internal record PullResponse(Type EntityType, string QueryId, IEnumerable<object> Items, long TotalRequestItems, long TotalItemsProcessed, bool Completed, Exception? Exception = null);
}
