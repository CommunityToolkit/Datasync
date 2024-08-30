// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Paging;
using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Service;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The concrete implementation of the <see cref="IDatasyncServiceClient{TEntity}"/> interface.
/// for HTTP datasync services.
/// </summary>
/// <typeparam name="TEntity">The type of entity being processed by this service client.</typeparam>
internal class DatasyncServiceClient<TEntity> : IDatasyncServiceClient<TEntity> where TEntity : class
{
    /// <summary>
    /// Creates a new <see cref="DatasyncServiceClient{TEntity}"/> using default information based on
    /// the <see cref="HttpClientOptions"/> provided.
    /// </summary>
    /// <remarks>
    /// The default path is /tables/entityName as a relative URI to the Endpoint in the options.
    /// </remarks>
    /// <param name="options">The <see cref="HttpClientOptions"/> to use.</param>
    public DatasyncServiceClient(HttpClientOptions options)
        : this(new Uri($"/tables/{typeof(TEntity).Name.ToLowerInvariant()}", UriKind.Relative), new HttpClientFactory(options).CreateClient())
    {
    }

    /// <summary>
    /// Creates a new <see cref="DatasyncServiceClient{TEntity}"/> with the normal information required for 
    /// communicating with a datasync service, using the default JSON Serializer Options.
    /// </summary>
    /// <param name="endpoint">The endpoint of the table controller that processes the entity.</param>
    /// <param name="client">The <see cref="HttpClient"/> to use for communication.</param>
    /// <exception cref="UriFormatException">Thrown if the endpoint is not valid.</exception>
    public DatasyncServiceClient(Uri endpoint, HttpClient client)
        : this(endpoint, client, DatasyncSerializer.JsonSerializerOptions)
    {
    }

    /// <summary>
    /// Creates a new <see cref="DatasyncServiceClient{TEntity}"/> with the normal information required for 
    /// communicating with a datasync service.
    /// </summary>
    /// <param name="endpoint">The endpoint of the table controller that processes the entity.</param>
    /// <param name="client">The <see cref="HttpClient"/> to use for communication.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing content.</param>
    /// <exception cref="UriFormatException">Thrown if the endpoint is not valid.</exception>
    public DatasyncServiceClient(Uri endpoint, HttpClient client, JsonSerializerOptions serializerOptions)
    {
        endpoint = MakeAbsoluteUri(client.BaseAddress, endpoint);
        ThrowIf.IsNotValidEndpoint(endpoint, nameof(endpoint));
        ArgumentNullException.ThrowIfNull(client, nameof(client));
        ArgumentNullException.ThrowIfNull(serializerOptions, nameof(serializerOptions));

        Endpoint = endpoint;
        Client = client;
        JsonSerializerOptions = serializerOptions;
    }

    /// <summary>
    /// The endpoint of the table controller that processes the entity.
    /// </summary>
    internal Uri Endpoint { get; }

    /// <summary>
    /// The <see cref="HttpClient"/> to use for communication.
    /// </summary>
    internal HttpClient Client { get; }

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing content.
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// The media type for application/json.
    /// </summary>
    internal MediaTypeHeaderValue jsonMediaType = MediaTypeHeaderValue.Parse("application/json");

    /// <summary>
    /// Creates a service client from another service client, only changing the type of the entity.
    /// </summary>
    /// <typeparam name="U">The new type of the entity.</typeparam>
    /// <returns>The replaced service client.</returns>
    public IReadOnlyDatasyncServiceClient<U> ToServiceClient<U>() where U : class
        => new DatasyncServiceClient<U>(Endpoint, Client, JsonSerializerOptions);

    /// <summary>
    /// Adds an entity to the remote service dataset.
    /// </summary>
    /// <param name="entity">The entity to be added to the remote service dataset.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response containing the added entity.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if the entity already exists in the remote service dataset.</exception>
    public async ValueTask<ServiceResponse<TEntity>> AddAsync(TEntity entity, DatasyncServiceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        EntityMetadata metadata = EntityResolver.GetEntityMetadata<TEntity>(entity);
        ThrowIf.EntityIdIsInvalid(metadata.Id, nameof(metadata), because: "The value of the 'Id' property must be null or valid.", allowNull: true);
        ThrowIf.IsNotNullOrEmpty(metadata.Version, nameof(metadata), "The value of the 'Version' property must be null or empty.");
        ThrowIf.IsNotNull(metadata.UpdatedAt, nameof(metadata), "The value of the 'UpdatedAt' property must be null.");

        using HttpRequestMessage request = new(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create<TEntity>(entity, this.jsonMediaType, JsonSerializerOptions)
        };

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        ServiceResponse<TEntity> result = await ServiceResponse<TEntity>.CreateAsync(response, JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        result.ThrowIfConflict(entity);
        result.ThrowIfNotSuccessful(requireContent: true);
        return result;
    }

    /// <summary>
    /// Creates a new <see cref="IDatasyncQueryable{TEntity}"/> for the current table.
    /// </summary>
    /// <returns>An <see cref="IDatasyncQueryable{TEntity}"/> the will return all items in the current table.</returns>
    public IDatasyncQueryable<TEntity> AsQueryable()
        => new DatasyncQueryable<TEntity>(this, Array.Empty<TEntity>().AsQueryable(), new Dictionary<string, string>(), false);

    /// <summary>
    /// Asynchronously returns the number of entities that will be returned by the provided query.
    /// </summary>
    /// <param name="query">Tne query to execute.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    public async ValueTask<ServiceResponse<int>> CountAsync(IDatasyncQueryable<TEntity> query, DatasyncServiceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        QueryDescription queryDescription = new QueryTranslator<TEntity>(query).Translate();
        
        // Make our query as efficient as possible for the specific task.
        queryDescription.Ordering.Clear();
        queryDescription.Selection.Clear();
        queryDescription.Skip = 0;
        queryDescription.Top = 0;

        // Make sure we request the total count
        queryDescription.RequestTotalCount = true;

        ServiceResponse<Page<TEntity>> result = await GetPageAsync(queryDescription.ToODataQueryString(), options, cancellationToken).ConfigureAwait(false);
        result.ThrowIfNotSuccessful(requireContent: true);
        return new ServiceResponse<int>(result) { Value = (int)GetCountOrThrow(result) };
    }

    /// <summary>
    /// Asynchronously returns the requested entity.
    /// </summary>
    /// <param name="id">Tne globally unique ID of the entity to be retrieved.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the requested entity.</returns>
    public async ValueTask<ServiceResponse<TEntity>> GetAsync(string id, DatasyncServiceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ThrowIf.EntityIdIsInvalid(id, nameof(id), because: "The entity ID must be valid.");

        Uri requestUri = BuildUri(id, options);
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        ServiceResponse<TEntity> result = await ServiceResponse<TEntity>.CreateAsync(response, JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        if (IsNotFound(id, result, options))
        {
            return result;
        }
        
        result.ThrowIfNotSuccessful(requireContent: true);
        return result;
    }

    /// <summary>
    /// Asynchronously returns a single page of results based on the provided query.
    /// </summary>
    /// <param name="query">The query to execute on the service.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns></returns>
    public async ValueTask<ServiceResponse<Page<TEntity>>> GetPageAsync(string query, DatasyncServiceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        Uri requestUri = new UriBuilder(Endpoint) { Query = query }.Uri;
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        ServiceResponse<Page<TEntity>> result = await ServiceResponse<Page<TEntity>>.CreateAsync(response, JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        result.ThrowIfNotSuccessful(requireContent: true);
        return result;
    }

    /// <summary>
    /// Ensure the query will get the deleted records.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> IncludeDeletedItems(bool enabled = true)
        => AsQueryable().IncludeDeletedItems(enabled);

    /// <summary>
    /// Ensure the query will get the total count for all the records that would have been returned
    /// ignoring any take paging/limit clause specified by client or server.
    /// </summary>
    /// <param name="enabled">If <c>true</c>, enables this request.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> IncludeTotalCount(bool enabled = true)
        => AsQueryable().IncludeTotalCount(enabled);

    /// <summary>
    /// Asynchronously returns the number of entities that will be returned by the provided query.
    /// </summary>
    /// <param name="query">Tne query to execute.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response containing the count of entities that will be returned by the provided query.</returns>
    public async ValueTask<ServiceResponse<long>> LongCountAsync(IDatasyncQueryable<TEntity> query, DatasyncServiceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        QueryDescription queryDescription = new QueryTranslator<TEntity>(query).Translate();

        // Make our query as efficient as possible for the specific task.
        queryDescription.Ordering.Clear();
        queryDescription.Selection.Clear();
        queryDescription.Skip = 0;
        queryDescription.Top = 0;

        // Make sure we request the total count
        queryDescription.RequestTotalCount = true;

        ServiceResponse<Page<TEntity>> result = await GetPageAsync(queryDescription.ToODataQueryString(), options, cancellationToken).ConfigureAwait(false);
        result.ThrowIfNotSuccessful(requireContent: true);
        return new ServiceResponse<long>(result) { Value = GetCountOrThrow(result) };
    }

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        => AsQueryable().OrderBy(keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        => AsQueryable().OrderByDescending(keySelector);

    /// <summary>
    /// Returns the asynchronous list of entities matching the query.
    /// </summary>
    /// <param name="query">Tne query to execute.</param>
    /// <returns>An <see cref="IAsyncPageable{TEntity}"/> for the results that can be asynchronously iterated over.</returns>
    public IAsyncPageable<TEntity> Query(IDatasyncQueryable<TEntity> query)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        QueryDescription queryDescription = new QueryTranslator<TEntity>(query).Translate();
        Uri requestUri = new UriBuilder(Endpoint) { Query = queryDescription.ToODataQueryString() }.Uri;
        return new FuncAsyncPageable<TEntity>((string? nextLink) =>
            GetNextPageAsync(nextLink is null ? requestUri.PathAndQuery : $"{requestUri.AbsolutePath}?{nextLink}"));
    }

    /// <summary>
    /// Removes an entity from the remote service dataset.
    /// </summary>
    /// <param name="id">The globally unique ID for the entity to be removed from the remote service dataset.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    public async ValueTask<ServiceResponse> RemoveAsync(string id, DatasyncServiceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ThrowIf.EntityIdIsInvalid(id, nameof(id), because: "The entity ID must be valid.");

        Uri requestUri = BuildUri(id, options);
        using HttpRequestMessage request = new(HttpMethod.Delete, requestUri);
        if (!string.IsNullOrWhiteSpace(options.Version))
        {
            ThrowIf.IsInvalidETag(options.Version, nameof(options.Version), ServiceErrorMessages.InvalidVersion);
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{options.Version}\""));
        }

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
        {
            ServiceResponse<TEntity> conflictResult = await ServiceResponse<TEntity>.CreateAsync(response, JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            throw new ConflictException<TEntity>(null, conflictResult);
        }

        ServiceResponse result = new(response);
        if (IsNotFound(id, result, options))
        {
            return result;
        }

        result.ThrowIfNotSuccessful();
        return result;
    }

    /// <summary>
    /// Replaced an entity in the remote service dataset.
    /// </summary>
    /// <param name="entity">The entity to be replaced in the remote service dataset.</param>
    /// <param name="options">The options for the operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A service response containing the replaced entity.</returns>
    /// <exception cref="ConflictException{TEntity}">Thrown if a version is provided and does not match the service version in the remote service dataset.</exception>
    public async ValueTask<ServiceResponse<TEntity>> ReplaceAsync(TEntity entity, DatasyncServiceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        EntityMetadata metadata = EntityResolver.GetEntityMetadata<TEntity>(entity);
        ThrowIf.EntityIdIsInvalid(metadata.Id, nameof(metadata), because: "The value of the 'Id' property must be null or valid.");

        Uri requestUri = BuildUri(metadata.Id!, options);
        using HttpRequestMessage request = new(HttpMethod.Put, requestUri)
        {
            Content = JsonContent.Create<TEntity>(entity, this.jsonMediaType, JsonSerializerOptions)
        };
        if (!string.IsNullOrWhiteSpace(options.Version))
        {
            ThrowIf.IsInvalidETag(options.Version, nameof(options.Version), ServiceErrorMessages.InvalidVersion);
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{options.Version}\""));
        }

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        ServiceResponse<TEntity> result = await ServiceResponse<TEntity>.CreateAsync(response, JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        if (IsNotFound(metadata.Id!, result, options))
        {
            return result;
        }

        result.ThrowIfConflict(entity);
        result.ThrowIfNotSuccessful(requireContent: true);
        return result;
    }

    /// <summary>
    /// Applies the specified selection to the source query.
    /// </summary>
    /// <typeparam name="U">Type representing the projected result of the query.</typeparam>
    /// <param name="selector">The selector function.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<U> Select<U>(Expression<Func<TEntity, U>> selector) where U : class
        => AsQueryable().Select(selector);

    /// <summary>
    /// Applies the specified skip clause to the source query.
    /// </summary>
    /// <param name="count">The number to skip.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> Skip(int count)
        => AsQueryable().Skip(count);

    /// <summary>
    /// Applies the specified take clause to the source query.
    /// </summary>
    /// <param name="count">The number to take.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> Take(int count)
        => AsQueryable().Take(count);

    /// <summary>
    /// Applies the specified ascending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        => AsQueryable().ThenBy(keySelector);

    /// <summary>
    /// Applies the specified descending order clause to the source query.
    /// </summary>
    /// <typeparam name="TKey">The type of the member being ordered by.</typeparam>
    /// <param name="keySelector">The expression selecting the member to order by.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        => AsQueryable().ThenByDescending(keySelector);

    /// <summary>
    /// Applies the specified filter predicate to the source query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        => AsQueryable().Where(predicate);

    /// <summary>
    /// Adds the parameter to the list of user-defined parameters to send with the request.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> WithParameter(string key, string value)
        => AsQueryable().WithParameter(key, value);

    /// <summary>
    /// Applies to the source query the specified string key-value pairs to be used as user-defined parameters with the request URI query string.
    /// </summary>
    /// <param name="parameters">The parameters to apply.</param>
    /// <returns>The composed query object.</returns>
    public IDatasyncQueryable<TEntity> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters)
        => AsQueryable().WithParameters(parameters);

    /// <summary>
    /// Builds the URI for a specific ID.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="options">Any specific options that should be used.</param>
    /// <returns>The request URI for the ID.</returns>
    internal Uri BuildUri(string id, DatasyncServiceOptions options)
    {
        UriBuilder builder = new(Endpoint);

        List<string> queryParts = [];
        if (options.IncludeDeleted)
        {
            queryParts.Add($"{ODataQueryParameters.IncludeDeleted}=true");
        }

        builder.Path = $"{builder.Path.TrimEnd('/')}/{id}";
        builder.Query = string.Join("&", queryParts);
        builder.Fragment = string.Empty;
        return builder.Uri;
    }

    /// <summary>
    /// Helper method to convert the total count from a page response into a value,
    /// throwing the right error if it didn't get returned.
    /// </summary>
    /// <param name="response">The <see cref="ServiceResponse{TEntity}"/> object.</param>
    /// <returns>The long count.</returns>
    /// <exception cref="DatasyncException">If the <see cref="ServiceResponse{TEntity}"/> does not contain the count.</exception>
    internal static long GetCountOrThrow(ServiceResponse<Page<TEntity>> response)
    {
        if (response.Value?.Count == null)
        {
            throw new DatasyncException(ServiceErrorMessages.InvalidContent);
        }

        return (long)response.Value.Count;
    }

    /// <summary>
    /// A helper method for the logic of the 404 Handling.
    /// </summary>
    /// <param name="id">The ID being requested.</param>
    /// <param name="result">The result from the service.</param>
    /// <param name="options">The options passed into the operation.</param>
    /// <returns>true if the entity was not found.</returns>
    /// <exception cref="EntityDoesNotExistException">Thrown if the exception was requested instead.</exception>
    internal bool IsNotFound(string id, ServiceResponse result, DatasyncServiceOptions options)
    {
        if (result.StatusCode != 404)
        {
            return false;
        }

        if (options.ThrowIfMissing)
        {
            throw new EntityDoesNotExistException(result, Endpoint, id);
        }

        return true;
    }

    /// <summary>
    /// The paging function for a paged query.
    /// </summary>
    /// <param name="queryOrContinuationToken">During the first run, this will be the query; thereafter, it will be the continuation token.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A page of results from the service.</returns>
    internal async ValueTask<Page<TEntity>> GetNextPageAsync(string queryOrContinuationToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryOrContinuationToken, nameof(queryOrContinuationToken));
        UriBuilder requestUriBuilder = new(Endpoint);
        if (queryOrContinuationToken.Contains('?'))
        {
            string[] parts = queryOrContinuationToken.Split('?');
            requestUriBuilder.Path = parts[0];
            requestUriBuilder.Query = parts[1];
        }
        else
        {
            requestUriBuilder.Path = queryOrContinuationToken;
        }

        using HttpRequestMessage request = new(HttpMethod.Get, requestUriBuilder.Uri);
        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        ServiceResponse<Page<TEntity>> result = await ServiceResponse<Page<TEntity>>.CreateAsync(response, JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        result.ThrowIfNotSuccessful(requireContent: true);
        return result.Value!;
    }

    /// <summary>
    /// Converts a base address + relative/absolute URI into the appropriate URI for the datasync service.
    /// </summary>
    /// <param name="baseAddress">The base address from the client.</param>
    /// <param name="relativeOrAbsoluteUri">A relative or absolute URI</param>
    /// <returns></returns>
    internal static Uri MakeAbsoluteUri(Uri? baseAddress, Uri relativeOrAbsoluteUri)
    {
        if (relativeOrAbsoluteUri.IsAbsoluteUri)
        {
            return new Uri($"{relativeOrAbsoluteUri.ToString().TrimEnd('/')}/");
        }

        if (baseAddress != null)
        {
            if (baseAddress.IsAbsoluteUri)
            {
                return new Uri($"{new Uri(baseAddress, relativeOrAbsoluteUri).ToString().TrimEnd('/')}/");
            }
        }

        throw new UriFormatException("Invalid combination of baseAddress and relativeUri");
    }
}
