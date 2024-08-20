// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;

namespace CommunityToolkit.Datasync.Client.Offline.Internal;

/// <summary>
/// The options to use for offline operations.
/// </summary>
internal class OfflineOptions()
{

    /// <summary>
    /// The list of entity client names.
    /// </summary>
    internal Dictionary<Type, EntityOptions> _cache = [];

    /// <summary>
    /// The <see cref="HttpClient"/> to use for this request.
    /// </summary>
    public required IHttpClientFactory HttpClientFactory { get; init; }

    /// <summary>
    /// Adds an entity to the mapping of options.
    /// </summary>
    /// <param name="entityType">The type of the entity being stored.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="endpoint">The endpoint serving the datasync services.</param>
    /// <param name="queryDescription">The optional query description to describe what entities need to be pulled.</param>
    public void AddEntity(Type entityType, string clientName, Uri endpoint, QueryDescription? queryDescription = null)
    {
        this._cache.Add(entityType, new EntityOptions { ClientName = clientName, Endpoint = endpoint, QueryDescription = queryDescription });
    }

    /// <summary>
    /// Returns a <see cref="HttpClient"/> for communicating with the remote service.
    /// </summary>
    /// <param name="entityType">The entity type being synchronized.</param>
    /// <returns>The <see cref="HttpClient"/> to use for communication.</returns>
    public HttpClient GetClient(Type entityType)
        => HttpClientFactory.CreateClient(this._cache[entityType].ClientName.AsNullableEmptyString());

    /// <summary>
    /// Returns a <see cref="Uri"/> for communicating with the remote service.
    /// </summary>
    /// <param name="entityType">The entity type being synchronized.</param>
    /// <returns>The <see cref="Uri"/> to use for synchronization operations.</returns>
    public Uri GetEndpoint(Type entityType)
        => this._cache.TryGetValue(entityType, out EntityOptions? options) ? options.Endpoint : new Uri("", UriKind.Relative);

    /// <summary>
    /// Returns a <see cref="QueryDescription"/> for client-side filtering of a pull operation.
    /// </summary>
    /// <param name="entityType">The entity type being synchronized.</param>
    /// <returns>The <see cref="QueryDescription"/> describing the client-side filtering of a pull operation.</returns>
    public QueryDescription GetQuery(Type entityType)
        => this._cache.GetValueOrDefault(entityType)?.QueryDescription ?? new QueryDescription();

    /// <summary>
    /// An internal structure for keeping the entity data.
    /// </summary>
    internal class EntityOptions
    {
        /// <summary>
        /// The name of the client to use when requesting a <see cref="HttpClient"/>.
        /// </summary>
        public required string ClientName { get; set; }

        /// <summary>
        /// The endpoint for the entity type.
        /// </summary>
        public required Uri Endpoint { get; set; }

        /// <summary>
        /// The query description to use - set to null for "all entities in the data set"
        /// </summary>
        internal QueryDescription? QueryDescription { get; set; }
    }
}
