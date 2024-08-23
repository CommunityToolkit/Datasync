// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using System.Security.Cryptography;
using System.Text;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The builder for a set of entity pull requests.
/// </summary>
public class PullRequestBuilder
{
    internal readonly PullOptions pullOptions = new();
    internal readonly Dictionary<string, PullRequest> requests = [];
    internal readonly List<Type> synchronizableTypes;
    internal readonly OfflineOptions offlineOptions;

    internal PullRequestBuilder(IEnumerable<Type> synchronizableTypes, OfflineOptions offlineOptions)
    {
        this.synchronizableTypes = synchronizableTypes.ToList();
        this.offlineOptions = offlineOptions;
    }

    /// <summary>
    /// Sets the number of parallel operations to use in communicating with the remote service.
    /// </summary>
    /// <param name="parallelOperations">The number of parallel operations</param>
    /// <returns>The builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of parallel operations is invalid.</exception>
    public PullRequestBuilder SetParallelOperations(int parallelOperations)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parallelOperations, nameof(parallelOperations));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(parallelOperations, 8, nameof(parallelOperations));
        this.pullOptions.ParallelOperations = parallelOperations;
        return this;
    }

    /// <summary>
    /// Adds a pull request for the default query as setup in the <see cref="OfflineDbContext.OnDatasyncInitialization(DatasyncOfflineOptionsBuilder)"/>
    /// method of your database context.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public PullRequestBuilder AddPullRequest<TEntity>() where TEntity : class
    {
        if (!this.synchronizableTypes.Contains(typeof(TEntity)))
        {
            throw new DatasyncException($"The type '{typeof(TEntity).Name}' is not synchronizable.");
        }

        EntityDatasyncOptions options = this.offlineOptions.GetOptions(typeof(TEntity));
        PullRequest request = new()
        {
            Endpoint = options.Endpoint,
            EntityType = typeof(TEntity),
            HttpClient = options.HttpClient,
            QueryDescription = options.QueryDescription,
            QueryId = GetQueryIdFromQuery(typeof(TEntity), options.QueryDescription)
        };
        this.requests[request.QueryId] = request;
        return this;
    }

    /// <summary>
    /// Adds a pull request for a query to a remote service.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity.</typeparam>
    /// <param name="configureAction">The configure action for configuring the pull request.</param>
    /// <returns>The builder for chaining.</returns>
    public PullRequestBuilder AddPullRequest<TEntity>(Action<PullRequest<TEntity>> configureAction) where TEntity : class
    {
        if (!this.synchronizableTypes.Contains(typeof(TEntity)))
        {
            throw new DatasyncException($"The type '{typeof(TEntity).Name}' is not synchronizable.");
        }

        EntityDatasyncOptions options = this.offlineOptions.GetOptions(typeof(TEntity));
        PullRequest<TEntity> request = new()
        {
            Endpoint = options.Endpoint,
            EntityType = typeof(TEntity),
            HttpClient = options.HttpClient,
            Query = new DatasyncPullQuery<TEntity>(),
            QueryId = string.Empty
        };
        configureAction(request);
        QueryDescription queryDescription = new QueryTranslator<TEntity>(request.Query).Translate();
        PullRequest storedRequest = new()
        {
            Endpoint = request.Endpoint,
            EntityType = request.EntityType,
            HttpClient = request.HttpClient,
            QueryDescription = queryDescription,
            QueryId = string.IsNullOrEmpty(request.QueryId) ? GetQueryIdFromQuery(request.EntityType, queryDescription) : request.QueryId
        };
        this.requests[storedRequest.QueryId] = storedRequest;
        return this;
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
    /// Builds the options for a pull operation.
    /// </summary>
    /// <returns>A <see cref="PullOptions"/> object.</returns>
    internal PullOptions BuildOptions()
        => this.pullOptions;

    /// <summary>
    /// Builds the list of pull requests.
    /// </summary>
    /// <returns>The list of pull requests.</returns>
    internal IEnumerable<PullRequest> BuildPullRequests()
        => this.requests.Values;
}
