// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Context;

/// <summary>
/// The settings to use for communicating with the datasync service.
/// </summary>
/// <param name="entityContextMap">The entity context map.</param>
/// <param name="clientFactory">The client factory for generating <see cref="HttpClient"/> instances.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options to use in serializing and deserializing entities.</param>
internal class DatasyncContext(IReadOnlyDictionary<Type, DatasyncEntityContext> entityContextMap, IHttpClientFactory clientFactory, JsonSerializerOptions jsonSerializerOptions)
{
    /// <summary>
    /// The entity context map.  This maps the entity type to the settings for that entity.
    /// </summary>
    internal IReadOnlyDictionary<Type, DatasyncEntityContext> EntityContextMap { get; } = entityContextMap;

    /// <summary>
    /// The client factory for generating <see cref="HttpClient"/> instances.
    /// </summary>
    internal IHttpClientFactory HttpClientFactory { get; } = clientFactory;

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use in serializing and deserializing entities.
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; } = jsonSerializerOptions;

    /// <summary>
    /// Returns the entity context for an entity type, or throws an error.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>The entity context.</returns>
    internal DatasyncEntityContext GetEntityContext<TEntity>() where TEntity : class
        => GetEntityContext(typeof(TEntity));

    /// <summary>
    /// Returns the entity context for an entity type, or throws an error.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <returns>The entity context.</returns>
    internal DatasyncEntityContext GetEntityContext(Type entityType)
    {
        if (EntityContextMap.TryGetValue(entityType, out DatasyncEntityContext? entityContext))
        {
            return entityContext;
        }

        throw new DatasyncException($"Invalid entity type for data synchronization: ${entityType.Name}");
    }

    /// <summary>
    /// Creates a datasync client for the provided entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to use.</typeparam>
    /// <returns>A datasync client that can be used to access the entity type on the remote service.</returns>
    /// <exception cref="DatasyncException">Thrown if the datasync client cannot be created.</exception>
    internal IDatasyncClient<TEntity> CreateDatasyncClient<TEntity>() where TEntity : class
    {
        DatasyncEntityContext entityContext = GetEntityContext(typeof(TEntity));
        HttpClient httpClient = HttpClientFactory.CreateClient(entityContext.HttpClientName);
        if (entityContext.Endpoint.IsAbsoluteUri)
        {
            return new DatasyncHttpClient<TEntity>(entityContext.Endpoint, httpClient, JsonSerializerOptions);
        }

        if (httpClient.BaseAddress is not null && httpClient.BaseAddress.IsAbsoluteUri)
        {
            return new DatasyncHttpClient<TEntity>(new Uri(httpClient.BaseAddress, entityContext.Endpoint), httpClient, JsonSerializerOptions);
        }

        throw new DatasyncException($"Cannot create absolute URI to datasync service for entity {typeof(TEntity).Name}");
    }
}
