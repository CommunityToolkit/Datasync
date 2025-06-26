// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;

namespace CommunityToolkit.Datasync.Client.Offline.Models;
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
    /// The default <see cref="IConflictResolver"/> to use for this request.
    /// </summary>
    public IConflictResolver? DefaultConflictResolver { get; set; }

    /// <summary>
    /// Adds an entity to the mapping of options.
    /// </summary>
    /// <param name="entityType">The type of the entity being stored.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="conflictResolver">The conflict resolver to use.</param>
    /// <param name="endpoint">The endpoint serving the datasync services.</param>
    /// <param name="queryDescription">The optional query description to describe what entities need to be pulled.</param>
    public void AddEntity(Type entityType, string clientName, IConflictResolver? conflictResolver, Uri endpoint, QueryDescription? queryDescription = null)
    {
        this._cache.Add(entityType, new EntityOptions 
        { 
            ClientName = clientName, 
            ConflictResolver = conflictResolver,
            Endpoint = endpoint, 
            QueryDescription = queryDescription
        });
    }

    /// <summary>
    /// Gets the options to use for communicating with a remote datasync service.
    /// </summary>
    /// <param name="entityType">The entity type for communication selection.</param>
    /// <returns>The datasync options.</returns>
    public EntityDatasyncOptions GetOptions(Type entityType)
    {
        if (this._cache.TryGetValue(entityType, out EntityOptions? options))
        {
            return new()
            {
                ConflictResolver = options.ConflictResolver ?? DefaultConflictResolver,
                Endpoint = options.Endpoint,
                HttpClient = HttpClientFactory.CreateClient(options.ClientName),
                QueryDescription = options.QueryDescription ?? new QueryDescription()
            };
        }
        else
        {
            return new()
            {
                ConflictResolver = DefaultConflictResolver,
                Endpoint = new Uri($"tables/{entityType.Name.ToLowerInvariant()}", UriKind.Relative),
                HttpClient = HttpClientFactory.CreateClient(),
                QueryDescription = new QueryDescription()
            };
        }
    }

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
        /// The conflict resolver for the entity options.
        /// </summary>
        internal IConflictResolver? ConflictResolver { get; set; }

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
