// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Contract;
using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// Provides the operations that can be performed against a remote dataset.
/// </summary>
/// <typeparam name="T">The type of entity stored in the remote dataset.</typeparam>
public class RemoteDataset<T> : IRemoteDataset<T>
{
    private const string JsonMediaType = "application/json";

    /// <summary>
    /// Creates a new <see cref="RemoteDataset{T}"/> using the standard options.
    /// </summary>
    /// <param name="endpoint">The datasync service endpoint for the table.</param>
    public RemoteDataset(Uri endpoint) : this(endpoint, new RemoteDatasetOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="RemoteDataset{T}"/> using the specified options.
    /// </summary>
    /// <param name="endpoint">The datasync service endpoint for the table.</param>
    /// <param name="options">The options to use in communicating with the remote dataset.</param>
    public RemoteDataset(Uri endpoint, IRemoteDatasetOptions options)
    {
        Ensure.That(endpoint, nameof(endpoint)).IsDatasyncEndpoint();
        Ensure.That(options, nameof(options)).IsNotNull();

        Endpoint = NormalizeEndpoint(endpoint);
        EntityContractService = new EntityContractService<T>(options.JsonSerializerOptions);
        EntityContractService.ValidateEntityType();

        if (options.HttpClient is not null)
        {
            HttpClient = options.HttpClient;
        }
        else if (options.HttpClientFactory is not null)
        {
            string clientName = options.HttpClientName?.Trim();
            if (string.IsNullOrEmpty(clientName))
            {
                clientName = typeof(T).Name.ToLowerInvariant();
            }

            HttpClient = options.HttpClientFactory.CreateClient(clientName);
        }
        else
        {
            DatasyncHttpClientFactory factory = new(options);
            HttpClient = factory.CreateClient(typeof(T).Name.ToLowerInvariant());
        }
    }

    /// <summary>
    /// A constructor for creating a duplicate of the current dataset with a different entity type.
    /// </summary>
    /// <param name="endpoint">The datasync service endpoint for the table.</param>
    /// <param name="client">The <see cref="HttpClient"/> to use.</param>
    /// <param name="entityContractService">The <see cref="EntityContractService"/> to use in working with the service entities.</param>
    private RemoteDataset(Uri endpoint, HttpClient client, EntityContractService entityContractService)
    {
        Endpoint = NormalizeEndpoint(endpoint);
        HttpClient = client;
        EntityContractService = new EntityContractService<T>(entityContractService.JsonSerializerOptions);
    }

    /// <summary>
    /// The datasync service endpoint for the table.
    /// </summary>
    public Uri Endpoint { get; }

    /// <summary>
    /// The <see cref="HttpClient"/> to use for communication with the remote dataset.
    /// </summary>
    internal HttpClient HttpClient { get; }

    /// <summary>
    /// The EntityContractService is used for handling entity-specific serialization and deserialization jobs.
    /// </summary>
    internal EntityContractService<T> EntityContractService { get; }

    /// <summary>
    /// Adds a conditional header based on the version string.
    /// </summary>
    /// <param name="force">If true, the header is not added.</param>
    /// <param name="request">The request to add the header to.</param>
    /// <param name="headerName">The name of the header.</param>
    /// <param name="version">The version string.</param>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot add non-generic static mether to generic static class.")]
    internal void AddConditionalHeader(bool force, HttpRequestMessage request, string headerName, string version)
    {
        if (!force && !string.IsNullOrWhiteSpace(version))
        {
            request.Headers.Add(headerName, $"\"{version}\"");
        }
    }

    /// <summary>
    /// Generates an appropriate exception for a remote service error response.
    /// </summary>
    /// <param name="response">The error response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An exception matching the error response.</returns>
    internal async ValueTask<RemoteDatasetException> GenerateExceptionForErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        Ensure.That(response, nameof(response)).IsNotNull();

        // This switch statement handles all the expected and known error codes.
        switch (response.StatusCode)
        {
            case HttpStatusCode.PreconditionFailed:
            case HttpStatusCode.Conflict:
                T entity = await ReadEntityFromResponseAsync(response, cancellationToken).ConfigureAwait(false);
                return new ConflictException<T>(response.ReasonPhrase, entity) { StatusCode = response.StatusCode };
            default:
                return new RemoteDatasetException(response.ReasonPhrase) { StatusCode = response.StatusCode };
        }
    }

    /// <summary>
    /// Reads and deserializes the entity from the provided response.
    /// </summary>
    /// <param name="response">The response that is being read.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The deserialized entity.</returns>
    /// <exception cref="RemoteDatasetException">If any errors occur.</exception>
    internal async ValueTask<T> ReadEntityFromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        Ensure.That(response, nameof(response)).IsNotNull();
        try
        {
            string entityJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return EntityContractService.DeserializeEntity(entityJson);
        }
        catch (JsonException ex)
        {
            throw new RemoteDatasetException("Entity cannot be deserialized from response", ex);
        }
    }

    /// <summary>
    /// Normalizes the endpoint for the dataset.  It must have a trailing slash on it.
    /// </summary>
    /// <param name="endpoint">The endpoint to normalize.</param>
    /// <returns>The normalized endpoint/</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Lessor of two linter warnings")]
    internal Uri NormalizeEndpoint(Uri endpoint)
    {
        UriBuilder builder = new(endpoint) { Query = string.Empty, Fragment = string.Empty };
        builder.Path = builder.Path.TrimEnd('/') + "/";
        return builder.Uri;
    }

    #region IReadonlyRemoteDataset<T> implementation
    /// <summary>
    /// Converts the current dataset to a dataset of a different type, but using the
    /// same basic settings.
    /// </summary>
    /// <typeparam name="U">The new entity type, which should be a subset of the current entity type.</typeparam>
    /// <returns>A <see cref="IReadonlyRemoteDataset{T}"/> to access the new entity type.</returns>
    public IReadonlyRemoteDataset<U> AsDataset<U>()
        => new RemoteDataset<U>(Endpoint, HttpClient, EntityContractService);

    /// <summary>
    /// Creates a new <see cref="IODataQuery{T}"/> object for the current dataset.
    /// </summary>
    /// <returns>A <see cref="IODataQuery{T}"/> to the current dataset.</returns>
    public IODataQuery<T> AsQueryable()
        => new ODataQuery<T>(this, Array.Empty<T>().AsQueryable(), new Dictionary<string, string>(), false);

    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The globally unique ID for the entity.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the entity requested when complete.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    public async ValueTask<T> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        Ensure.That(id, nameof(id)).IsNotNull().And.IsAValidId();

        HttpRequestMessage request = new(HttpMethod.Get, new Uri(Endpoint, id));
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return await ReadEntityFromResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw await GenerateExceptionForErrorResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Counts the number of entities that would be returned by the query.
    /// </summary>
    /// <param name="odataQueryString">The OData querry string to send to the service.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the number of entities matching the query when complete.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    public ValueTask<long> LongCountAsync(string odataQueryString, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Executes the query on the remote service, allowing the enumeration of the results asynchronously.
    /// </summary>
    /// <param name="odataQueryString">The OData query string to send to the service.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> for iterating through the entities.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    public IAsyncEnumerable<T> Query(string odataQueryString, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region IRemoteDataset<T> implementation
    /// <summary>
    /// Adds a new entity to the remote dataset.
    /// </summary>
    /// <param name="entity">The entity to add to the remote dataset.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the added entity (with all metadata set) when complete.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    public async ValueTask<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Ensure.That(entity, nameof(entity)).IsNotNull();
        EntityContractService.ValidateEntity(entity, allowNullIdentity: true);

        HttpRequestMessage request = new(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(EntityContractService.SerializeEntity(entity), Encoding.UTF8, JsonMediaType)
        };
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return await ReadEntityFromResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw await GenerateExceptionForErrorResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes an entity from the remote dataset.
    /// </summary>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    public ValueTask RemoveAsync(string id, CancellationToken cancellationToken = default)
        => RemoveAsync(id, "", cancellationToken);

    /// <summary>
    /// Removes an entity from the remote dataset if it matches the provided version.
    /// </summary>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <param name="version">The required version on the service side.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    /// <exception cref="RemoteDatasetException">Thrown if the remote service returns an error.</exception>
    public async ValueTask RemoveAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        Ensure.That(id, nameof(id)).IsNotNull().And.IsAValidId();

        HttpRequestMessage request = new(HttpMethod.Delete, new Uri(Endpoint, id));
        AddConditionalHeader(false, request, "If-Match", version);

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw await GenerateExceptionForErrorResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Replaces an entity with new data if the version in the replacement data matches the version on the service.
    /// </summary>
    /// <remarks>
    /// Do not alter the system metadata (ID, Deleted, UpdatedAt, Version) in the entity.
    /// </remarks>
    /// <param name="entity">The entity with new data.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the replaced entity (with all metadata set) when complete.</returns>
    public ValueTask<T> ReplaceAsync(T entity, CancellationToken cancellationToken = default)
        => ReplaceAsync(entity, false, cancellationToken);

    /// <summary>
    /// Replaces an entity with new data if the version in the replacement data matches the version on the service.
    /// </summary>
    /// <remarks>
    /// Do not alter the system metadata (ID, Deleted, UpdatedAt, Version) in the entity.
    /// </remarks>
    /// <param name="entity">The entity with new data.</param>
    /// <param name="force">If true, the version is not considered in replacing the service side.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the replaced entity (with all metadata set) when complete.</returns>
    public async ValueTask<T> ReplaceAsync(T entity, bool force, CancellationToken cancellationToken = default)
    {
        Ensure.That(entity, nameof(entity)).IsNotNull();
        EntityContractService.ValidateEntity(entity, allowNullIdentity: false);

        HttpRequestMessage request = new(HttpMethod.Put, new Uri(Endpoint, EntityContractService.GetId(entity)))
        {
            Content = new StringContent(EntityContractService.SerializeEntity(entity), Encoding.UTF8, JsonMediaType)
        };
        AddConditionalHeader(force, request, "If-Match", EntityContractService.GetOptimisticConcurrencyToken(entity));

        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return await ReadEntityFromResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw await GenerateExceptionForErrorResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }
    #endregion

    #region IODataLinqMethods<T> implementation
    /// <summary>
    /// Ensure the query will get the deleted records.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> IncludeDeletedItems(bool enabled = true)
        => AsQueryable().IncludeDeletedItems(enabled);

    /// <summary>
    /// Ensure the query will get the total count for all the records that would have been returned
    /// ignoring any take paging/limit clause specified by client or server.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this requst.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> IncludeTotalCount(bool enabled = true)
        => AsQueryable().IncludeTotalCount(enabled);

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => AsQueryable().OrderBy(keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => AsQueryable().OrderByDescending(keySelector);

    /// <summary>
    /// Applies the specified selection to the source query.
    /// </summary>
    /// <typeparam name="U">Type representing the projected result of the query.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<U> Select<U>(Expression<Func<T, U>> selector)
        => AsQueryable().Select(selector);

    /// <summary>
    /// Applies the specified skip clause to the source query.
    /// </summary>
    /// <param name="count">The number to skip.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> Skip(int count)
        => AsQueryable().Skip(count);

    /// <summary>
    /// Applies the specified take clause to the source query.
    /// </summary>
    /// <param name="count">The number to take.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> Take(int count)
        => AsQueryable().Take(count);

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => AsQueryable().ThenBy(keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => AsQueryable().ThenByDescending(keySelector);

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> Where(Expression<Func<T, bool>> predicate)
        => AsQueryable().Where(predicate);

    /// <summary>
    /// Adds the parameter to the list of user-defined parameters to send with the
    /// request.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>The composed query object.</returns>
    public IODataQuery<T> WithParameter(string key, string value)
        => AsQueryable().WithParameter(key, value);
    #endregion
}
