// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Context;
using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A builder pattern for the <see cref="DatasyncContext"/> object.
/// </summary>
public class DatasyncContextBuilder
{
    /// <summary>
    /// The mapping from entity types to the endpoints that serve those entity types.
    /// </summary>
    internal IDictionary<Type, DatasyncEntityContext> EntityContextMap { get; } = new Dictionary<Type, DatasyncEntityContext>();

    /// <summary>
    /// The mechanism by which we get <see cref="HttpClient"/> objects.
    /// </summary>
    internal IHttpClientFactory HttpClientFactory { get; set; } = new DatasyncHttpClientFactory(() => new HttpClient());

    /// <summary>
    /// The serializer options to use when communicating with a datasync service.
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; set; } = new DatasyncServiceOptions().JsonSerializerOptions;

    /// <summary>
    /// Creates a new <see cref="DatasyncContextBuilder"/> seeded with a set of entity types.
    /// </summary>
    /// <param name="entityTypes">The entity types to use for seeding the context builder.</param>
    public DatasyncContextBuilder(IEnumerable<Type> entityTypes)
    {
        foreach (Type entityType in entityTypes)
        {
            EntityContextMap.Add(entityType, new DatasyncEntityContext($"/tables/{entityType.Name.ToLowerInvariant()}s"));
        }
    }

    /// <summary>
    /// Builds the <see cref="DatasyncContext"/>.
    /// </summary>
    /// <returns>The <see cref="DatasyncContext"/> represented by this builder.</returns>
    internal DatasyncContext Build()
        => new((IReadOnlyDictionary<Type, DatasyncEntityContext>)EntityContextMap, HttpClientFactory, JsonSerializerOptions);

    /// <summary>
    /// Returns the <see cref="DatasyncEntityContext"/> for the specified entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The <see cref="DatasyncEntityContext"/> for the entity type.</returns>
    /// <exception cref="DatasyncContextException">Thrown if the entity does not exist.</exception>
    public DatasyncEntityContext Entity<TEntity>() where TEntity : class
        => Entity(typeof(TEntity));

    /// <summary>
    /// Returns the <see cref="DatasyncEntityContext"/> for the specified entity.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The <see cref="DatasyncEntityContext"/> for the entity type.</returns>
    /// <exception cref="DatasyncContextException">Thrown if the entity does not exist.</exception>
    public DatasyncEntityContext Entity(Type entityType)
    {
        if (EntityContextMap.TryGetValue(entityType, out DatasyncEntityContext? entityContext))
        {
            return entityContext;
        }

        throw new DatasyncContextException($"Entity {entityType.Name} is not a valid datasync entity.");
    }

    /// <summary>
    /// Configures the specified entity for datasync operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="configureAction">The configuration action.</param>
    /// <returns>The <see cref="DatasyncContextBuilder"/> for the database context..</returns>
    /// <exception cref="DatasyncContextException">Thrown if the entity does not exist.</exception>
    public DatasyncContextBuilder Entity<TEntity>(Action<DatasyncEntityContext> configureAction) where TEntity : class
        => Entity(typeof(TEntity), configureAction);

    /// <summary>
    /// Configures the specified entity for datasync operations.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="configureAction">The configuration action.</param>
    /// <returns>The <see cref="DatasyncContextBuilder"/> for the database context..</returns>
    /// <exception cref="DatasyncContextException">Thrown if the entity does not exist.</exception>
    public DatasyncContextBuilder Entity(Type entityType, Action<DatasyncEntityContext> configureAction)
    {
        if (EntityContextMap.TryGetValue(entityType, out DatasyncEntityContext? entityContext))
        {
            configureAction.Invoke(entityContext);
            return this;
        }

        throw new DatasyncContextException($"Entity {entityType.Name} is not a valid datasync entity.");
    }

    /// <summary>
    /// Sets the <see cref="HttpClient"/> to use for all communications based on a base address.
    /// </summary>
    /// <param name="endpoint">The URI of the datasync service.</param>
    /// <returns>The <see cref="DatasyncContextBuilder"/> for chaining.</returns>
    public DatasyncContextBuilder SetDatasyncEndpoint(string endpoint)
        => SetDatasyncEndpoint(new Uri(endpoint));

    /// <summary>
    /// Sets the <see cref="HttpClient"/> to use for all communications based on a base address.
    /// </summary>
    /// <param name="endpoint">The URI of the datasync service.</param>
    /// <returns>The <see cref="DatasyncContextBuilder"/> for chaining.</returns>
    public DatasyncContextBuilder SetDatasyncEndpoint(Uri endpoint)
    {
        _ = Ensure.That(endpoint, nameof(endpoint)).IsDatasyncEndpoint();
        HttpClientFactory = new DatasyncHttpClientFactory(() => new HttpClient() { BaseAddress = endpoint });
        return this;
    }

    /// <summary>
    /// Sets the <see cref="HttpClient"/> to use for all communication with the datasync service.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClientFactory"/> to use.</param>
    /// <returns>The <see cref="DatasyncContextBuilder"/> for chaining.</returns>
    public DatasyncContextBuilder SetHttpClient(HttpClient httpClient)
    {
        _ = Ensure.That(httpClient, nameof(httpClient)).IsNotNull();
        HttpClientFactory = new DatasyncHttpClientFactory(() => httpClient);
        return this;
    }

    /// <summary>
    /// Sets the <see cref="HttpClientFactory"/> to use for getting <see cref="HttpClient"/> entities
    /// to use for the communication with the datasync service.
    /// </summary>
    /// <param name="httpClientFactory">The <see cref="HttpClientFactory"/> to use.</param>
    /// <returns>The <see cref="DatasyncContextBuilder"/> for chaining.</returns>
    public DatasyncContextBuilder SetHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        HttpClientFactory = Ensure.That(httpClientFactory, nameof(httpClientFactory)).IsNotNull().Value;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="JsonSerializerOptions"/> to use for communicating with the datasync service.
    /// </summary>
    /// <remarks>
    /// You should never have to set this in a real environment since the defaults are specifically chosen
    /// to be compatible with the default serializer options used by the datasync service.
    /// </remarks>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns>The <see cref="DatasyncContextBuilder"/> for chaining.</returns>
    public DatasyncContextBuilder SetJsonSerializerOptions(JsonSerializerOptions jsonSerializerOptions)
    {
        JsonSerializerOptions = Ensure.That(jsonSerializerOptions, nameof(jsonSerializerOptions)).IsNotNull().Value;
        return this;
    }
}
