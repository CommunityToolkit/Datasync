// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline.Internal;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The options builder for the offline operations.
/// </summary>
public class DatasyncOfflineOptionsBuilder
{
    internal IHttpClientFactory? _httpClientFactory;
    internal readonly Dictionary<string, EntityOfflineOptions> _entities;

    /// <summary>
    /// Creates the builder based on the required entity types.
    /// </summary>
    /// <param name="entityTypes">The entity type list.</param>
    internal DatasyncOfflineOptionsBuilder(IEnumerable<Type> entityTypes)
    {
        this._entities = entityTypes.ToDictionary(x => x.FullName!, x => new EntityOfflineOptions(x));
    }

    /// <summary>
    /// Sets the default mechanism for getting a <see cref="HttpClient"/> to be
    /// the specified <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> to use.</param>
    /// <returns>The current builder for chaining.</returns>
    public DatasyncOfflineOptionsBuilder UseHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
        this._httpClientFactory = httpClientFactory;
        return this;
    }

    /// <summary>
    /// Sets the default mechanism for getting a <see cref="HttpClient"/> to be
    /// a constant <see cref="HttpClient"/>
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use.</param>
    /// <returns>The current builder for chaining.</returns>
    public DatasyncOfflineOptionsBuilder UseHttpClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        this._httpClientFactory = new BasicHttpClientFactory(httpClient);
        return this;
    }

    /// <summary>
    /// Sets the default mechanism for getting a <see cref="HttpClient"/> to be a
    /// standard <see cref="IHttpClientFactory"/> based on the provided endpoint.
    /// </summary>
    /// <param name="endpoint">The <see cref="Uri"/> pointing to the datasync endpoint.</param>
    /// <returns>The current builder for chaining.</returns>
    public DatasyncOfflineOptionsBuilder UseEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));
        ThrowIf.IsNotValidEndpoint(endpoint, nameof(endpoint));
        this._httpClientFactory = new HttpClientFactory(new HttpClientOptions { Endpoint = endpoint });
        return this;
    }

    /// <summary>
    /// Sets the default mechanism for getting a <see cref="HttpClient"/> to be a
    /// standard <see cref="IHttpClientFactory"/> based on the provided client options
    /// </summary>
    /// <param name="clientOptions">The <see cref="HttpClientOptions"/> pointing to the datasync endpoint.</param>
    /// <returns>The current builder for chaining.</returns>
    public DatasyncOfflineOptionsBuilder UseHttpClientOptions(HttpClientOptions clientOptions)
    {
        ArgumentNullException.ThrowIfNull(clientOptions, nameof(clientOptions));
        this._httpClientFactory = new HttpClientFactory(clientOptions);
        return this;
    }

    /// <summary>
    /// Configures the specified entity type for offline operations.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="configure">A configuration function for the entity.</param>
    /// <returns>The current builder for chaining.</returns>
    public DatasyncOfflineOptionsBuilder Entity<TEntity>(Action<EntityOfflineOptions> configure) 
        => Entity(typeof(TEntity), configure);

    /// <summary>
    /// Configures the specified entity type for offline operations.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="configure">A configuration function for the entity.</param>
    /// <returns>The current builder for chaining.</returns>
    public DatasyncOfflineOptionsBuilder Entity(Type entityType, Action<EntityOfflineOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(entityType, nameof(entityType));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));
        if (!this._entities.TryGetValue(entityType.FullName!, out EntityOfflineOptions? options))
        {
            throw new DatasyncException($"Entity is not synchronizable.");
        }

        configure(options);
        return this;
    }

    /// <summary>
    /// Converts the builder into a read-only set of options.
    /// </summary>
    /// <returns>The offline options built from this builder.</returns>
    internal OfflineOptions Build()
    {
        if (this._httpClientFactory == null)
        {
            throw new DatasyncException($"Datasync service connection is not set.");
        }

        OfflineOptions result = new()
        {
            HttpClientFactory = this._httpClientFactory
        };
        foreach (EntityOfflineOptions entity in this._entities.Values)
        {
            result.AddEntity(entity.EntityType, entity.ClientName, entity.Endpoint);
        }

        return result;
    }

    /// <summary>
    /// The entity offline options that are used for configuration.
    /// </summary>
    /// <param name="entityType">The entity type for this entity offline options.</param>
    public class EntityOfflineOptions(Type entityType)
    {
        /// <summary>
        /// The entity type being configured.
        /// </summary>
        public Type EntityType { get => entityType; }

        /// <summary>
        /// The endpoint for the entity type.
        /// </summary>
        public Uri Endpoint { get; set; } = new Uri($"/tables/{entityType.Name.ToLowerInvariant()}", UriKind.Relative);

        /// <summary>
        /// The name of the client to use when requesting a <see cref="HttpClient"/>.
        /// </summary>
        public string ClientName { get; set; } = string.Empty;
    }
}
