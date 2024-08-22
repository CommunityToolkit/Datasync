// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline.Operations;

/// <summary>
/// Operation manager for the pull operation.
/// </summary>
/// <param name="context">The database context to use for the pull operation.</param>
internal class PullOperationManager(OfflineDbContext context)
{
    /// <summary>
    /// Retrieves a single page of results from the service.  This throws an
    /// error if the response is invalid, and decodes the page of results.
    /// </summary>
    /// <param name="pageType">The page type (actual return type from the service).</param>
    /// <param name="client">The <see cref="HttpClient"/> to use.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A page of items.</returns>
    /// <exception cref="DatasyncPullException">Thrown on error</exception>
    internal async Task<Page<object>> GetPageAsync(Type pageType, HttpClient client, Uri requestUri, CancellationToken cancellationToken = default)
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
    /// Inner loop for the pull operation.
    /// </summary>
    /// <param name="entityType">The type of the entity being retrieved.</param>
    /// <param name="entityOptions">The options for synchronizing data to the service.</param>
    /// <param name="queryString">The initial query string.</param>
    /// <param name="options">The options for the pull operation (with QueryId set).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The pull result.</returns>
    internal async Task<PullResult> ExecutePullInnerLoopAsync(
        Type entityType, 
        EntityDatasyncOptions entityOptions, 
        string queryString, 
        PullOptions options,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset lastSynchronization = await context.DeltaTokenStore.GetDeltaTokenAsync(options.QueryId, cancellationToken).ConfigureAwait(false);
        Uri endpoint = ExecutableOperation.MakeAbsoluteUri(entityOptions.HttpClient.BaseAddress, entityOptions.Endpoint);
        Type pageType = typeof(Page<>).MakeGenericType(entityType);
        PullResult pullResult = new();

        QueueHandler<PulledItem> queueHandler = new(1, async pulledItem =>
        {
            EntityMetadata metadata = EntityResolver.GetEntityMetadata(pulledItem.Item, entityType);
            if (metadata.Deleted)
            {
                _ = context.Remove(pulledItem.Item);
            }
            else
            {
                object? originalEntry = await context.FindAsync(entityType, [metadata.Id], cancellationToken).ConfigureAwait(false);
                if (originalEntry is null)
                {
                    _ = context.Add(pulledItem.Item);
                }
                else
                {
                    _ = context.Update(pulledItem.Item);
                }
            }

            if (metadata.UpdatedAt.HasValue)
            {
                await SetDeltaTokenAsync(options.QueryId, lastSynchronization, metadata.UpdatedAt.Value, cancellationToken).ConfigureAwait(false);
            }
        });

        while (true)
        {
            Uri requestUri = new UriBuilder(endpoint) { Query = queryString }.Uri;

            try
            {
                Page<object> page = await GetPageAsync(pageType, entityOptions.HttpClient, requestUri, cancellationToken).ConfigureAwait(false);
                foreach (object item in page.Items)
                {
                    queueHandler.Enqueue(new PulledItem(item));
                }

                if (page.NextLink is not null)
                {
                    queryString = page.NextLink;
                }
            }
            catch (DatasyncPullException exception)
            {
                pullResult.AddFailedRequest(requestUri, exception.ServiceResponse);
                break;
            }
        }

        await queueHandler.WhenComplete().ConfigureAwait(false);
        return pullResult;
    }

    /// <summary>
    /// Obtain a query ID from the query and entity type.  This is used when the developer does
    /// not specify a query ID.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="query">The query,</param>
    /// <returns></returns>
    internal static string GetQueryIdFromQuery(Type entityType, QueryDescription query)
    {
        string odataQuery = query.ToODataQueryString();
        if (string.IsNullOrEmpty(odataQuery))
        {
            return entityType.FullName!;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(odataQuery);
        byte[] hashBytes = MD5.HashData(bytes);
        string queryId = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
        return $"q-{entityType.FullName!}-{queryId}";
    }

    /// <summary>
    /// Sets the delta token for the provided query ID if it's later than what's stored.
    /// </summary>
    /// <param name="queryId"></param>
    /// <param name="lastSync"></param>
    /// <param name="updatedAt"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    internal async Task SetDeltaTokenAsync(string queryId, DateTimeOffset lastSync, DateTimeOffset? updatedAt, CancellationToken cancellationToken = default)
    {
        if (updatedAt.HasValue && updatedAt.Value > lastSync)
        {
            await context.DeltaTokenStore.SetDeltaTokenAsync(queryId, updatedAt.Value, cancellationToken);
        }
    }

    /// <summary>
    /// Internal method to pull entities from a remote service.
    /// </summary>
    /// <param name="entityType">The entity type to pull.</param>
    /// <param name="query">A query to execute.</param>
    /// <param name="options">The options to use for pulling.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns></returns>
    public async Task<PullResult> PullAsync(Type entityType, QueryDescription query, PullOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType, nameof(entityType));
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        if (!context.QueueManager.GetSynchronizableEntityTypes().Contains(entityType))
        {
            throw new DatasyncException($"Type '{entityType.Name}' is not synchronizable.");
        }

        OfflineOptions offlineOptions = context.BuildDatasyncOfflineOptions();
        EntityDatasyncOptions entityOptions = offlineOptions.GetOptions(entityType);

        using IDisposable syncLock = await LockManager.AcquireSynchronizationLockAsync(cancellationToken).ConfigureAwait(false);

        int queueCount = await context.QueueManager.CountOperationsInQueueAsync(entityType, cancellationToken).ConfigureAwait(false);
        if (queueCount > 0)
        {
            throw new DatasyncException($"There are still pending operations in queue for table '{entityType.Name}'.");
        }

        options.QueryId = !string.IsNullOrEmpty(options.QueryId) ? options.QueryId : GetQueryIdFromQuery(entityType, query);
        DateTimeOffset lastSynchronization = await context.DeltaTokenStore.GetDeltaTokenAsync(options.QueryId, cancellationToken).ConfigureAwait(false);

        if (options.AlwaysPullWithDeltaToken || lastSynchronization.ToUnixTimeMilliseconds() > 0L)
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
        string queryString = query.ToODataQueryString();

        PullResult result = await ExecutePullInnerLoopAsync(entityType, entityOptions, queryString, options, cancellationToken).ConfigureAwait(false);
        _ = await context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// A wrapper around the pulled item.
    /// </summary>
    /// <param name="Item">The object version of the item.</param>
    internal record PulledItem(object Item);
}
