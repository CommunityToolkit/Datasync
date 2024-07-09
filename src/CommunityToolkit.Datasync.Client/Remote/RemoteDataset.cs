// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0290 // Use primary constructor

using CommunityToolkit.Datasync.Client.Models;
using CommunityToolkit.Datasync.Common;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace CommunityToolkit.Datasync.Client.Remote;

/// <summary>
/// A <see cref="RemoteDataset{T}"/> represents the collection of all entities in the remote dataset, and
/// provides methods for accessing and modifying the remote data set.  The remote dataset is created via
/// the <see cref="DatasyncClient"/> object.
/// </summary>
/// <typeparam name="T">The type of entity accessed via the remote dataset.</typeparam>
public class RemoteDataset<T> : IRemoteDataset<T> where T : notnull
{
    /// <summary>
    /// The application/json MIME type.
    /// </summary>
    private readonly MediaTypeHeaderValue jsonMediaType = new("application/json", "utf-8");

    /// <summary>
    /// The parameter name to include deleted items in the request.
    /// </summary>
    private const string IncludeDeletedParameter = "__includedeleted";

    /// <summary>
    /// Creates a new <see cref="RemoteDataset{T}"/> object using a specific client, options, and path.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use for accessing the remote dataset.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of JSON content.</param>
    /// <param name="path">The relative path to the table endpoint.</param>
    public RemoteDataset(HttpClient client, JsonSerializerOptions options, string path)
    {
        Client = Ensure.That(client, nameof(client)).IsNotNull().And.HasDatasyncEndpoint().Value;
        SerializerOptions = Ensure.That(options, nameof(options)).IsNotNull().Value;
        DatasetPath = "/" + Ensure.That(path, nameof(path)).IsHttpPath().Value.Trim('/');
    }

    /// <summary>
    /// The <see cref="HttpClient"/> to use for accessing the remote dataset.
    /// </summary>
    internal HttpClient Client { get; }

    /// <summary>
    /// The relative path to the table endpoint.
    /// </summary>
    internal string DatasetPath { get; }

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of JSON content.
    /// </summary>
    internal JsonSerializerOptions SerializerOptions { get; }

    #region IRemoteDataset<T>
    /// <inheritdoc />
    public async Task<T> AddAsync(T entity, RemoteOperationOptions options, CancellationToken cancellationToken = default)
    {
        _ = Ensure.That(entity, nameof(entity)).IsNotNull();
        _ = Ensure.That(EntityTypeCache.GetEntityId(entity), nameof(entity)).IsNullOrEntityId();

        using HttpRequestMessage requestMessage = new(HttpMethod.Post, DatasetPath)
        {
            Content = JsonContent.Create(entity, this.jsonMediaType, SerializerOptions)
        };

        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        if (!responseMessage.IsSuccessStatusCode)
        {
            if (responseMessage.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
            {
                throw await ConflictException<T>.CreateAsync(responseMessage, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw await DatasyncHttpException.CreateAsync(responseMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        return await GetJsonContentFromResponseAsync<T>(responseMessage.Content, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(string query, RemoteOperationOptions options, CancellationToken cancellationToken = default)
    {
        _ = Ensure.That(query, nameof(query)).IsNotNull();
        const string countQueryString = "$select=id&$skip=0&$top=1&$count=true";

        string queryString = BuildQueryString(query, options.IncludeDeletedItems ? $"{countQueryString}&{IncludeDeletedParameter}=true" : countQueryString);
        Page<T> result = await GetPageAsync($"{DatasetPath}{queryString}", cancellationToken).ConfigureAwait(false);
        return result.Count ?? throw new DatasyncException("Expected count return from service, but received null");
    }

    /// <inheritdoc />
    public async ValueTask<T> GetAsync(string id, RemoteOperationOptions options, CancellationToken cancellationToken = default)
    {
        _ = Ensure.That(id, nameof(id)).IsNotNull().And.IsEntityId();

        string relativeUri = options.IncludeDeletedItems ? $"{EntityPath(id)}?{IncludeDeletedParameter}=true" : EntityPath(id);
        using HttpRequestMessage requestMessage = new(HttpMethod.Get, relativeUri);
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        if (!responseMessage.IsSuccessStatusCode)
        {
            throw await DatasyncHttpException.CreateAsync(responseMessage, cancellationToken).ConfigureAwait(false);
        }

        return await GetJsonContentFromResponseAsync<T>(responseMessage.Content, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Page<T>> GetPageAsync(string pathAndQuery, CancellationToken cancellationToken = default)
    {
        _ = Ensure.That(pathAndQuery, nameof(pathAndQuery)).IsNotNullOrWhiteSpace();

        using HttpRequestMessage requestMessage = new(HttpMethod.Get, pathAndQuery);
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        if (!responseMessage.IsSuccessStatusCode) 
        {
            throw await DatasyncHttpException.CreateAsync(responseMessage, cancellationToken).ConfigureAwait(false);
        }

        return await GetJsonContentFromResponseAsync<Page<T>>(responseMessage.Content, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string id, RemoteOperationOptions options, CancellationToken cancellationToken = default)
    {
        _ = Ensure.That(id, nameof(id)).IsNotNull().And.IsEntityId();

        using HttpRequestMessage requestMessage = new(HttpMethod.Delete, EntityPath(id));
        AddConditionalHeader(requestMessage, options.RequiredVersion);

        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        if (!responseMessage.IsSuccessStatusCode)
        {
            if (responseMessage.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
            {
                throw await ConflictException<T>.CreateAsync(responseMessage, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw await DatasyncHttpException.CreateAsync(responseMessage, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<T> ReplaceAsync(T entity, RemoteOperationOptions options, CancellationToken cancellationToken = default)
    {
        _ = Ensure.That(entity, nameof(entity)).IsNotNull();
        string id = EntityTypeCache.GetEntityId(entity) ?? throw new ArgumentException("Id cannot be null", nameof(entity));
        _ = Ensure.That(id!, nameof(entity)).IsEntityId();

        using HttpRequestMessage requestMessage = new(HttpMethod.Put, EntityPath(id))
        {
            Content = JsonContent.Create(entity, this.jsonMediaType, SerializerOptions)
        };
        AddConditionalHeader(requestMessage, options.RequiredVersion);

        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        if (!responseMessage.IsSuccessStatusCode)
        {
            if (responseMessage.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
            {
                throw await ConflictException<T>.CreateAsync(responseMessage, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw await DatasyncHttpException.CreateAsync(responseMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        return await GetJsonContentFromResponseAsync<T>(responseMessage.Content, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    /// <summary>
    /// Adds a conditional header to the <paramref name="requestMessage"/> if the version is specified.
    /// </summary>
    /// <param name="requestMessage">The request message to modify.</param>
    /// <param name="version">The (optional) version.</param>
    internal static void AddConditionalHeader(HttpRequestMessage requestMessage, string? version)
    {
        if (!string.IsNullOrEmpty(version))
        {
            requestMessage.Headers.Add("If-Match", $"\"{version}\"");
        }
    }

    /// <summary>
    /// Builds a query string based on the source, but with the overrides that are provided.
    /// The query string starts with a '?' if anything is returned.
    /// </summary>
    /// <param name="source">The source query string.</param>
    /// <param name="overrides">The overrides query string.</param>
    /// <returns>A new query string.</returns>
    internal static string BuildQueryString(string source, string overrides)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(source);
        NameValueCollection oq = HttpUtility.ParseQueryString(overrides);
        foreach (string key in oq.Keys)
        {
            query[key] = oq[key];
        }

        string queryString = query.ToString()!.TrimStart('?');
        return string.IsNullOrEmpty(queryString) ? string.Empty : $"?{queryString}";
    }

    /// <summary>
    /// Returns the appropriate entity path for the given ID.
    /// </summary>
    /// <param name="id">The ID of the request.</param>
    /// <returns>The entity path for the request.</returns>
    internal string EntityPath(string id)
        => $"{DatasetPath}/{id}";

    /// <summary>
    /// Retrieves the response from the server as the provided type.
    /// </summary>
    /// <typeparam name="U">The type of the response.</typeparam>
    /// <param name="content">The <see cref="HttpContent"/> from the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The content from the server.</returns>
    /// <exception cref="DatasyncException">Thrown if the server response is invalid.</exception>
    /// <exception cref="JsonException">Thrown if the JSON content is bad.</exception>
    internal async Task<U> GetJsonContentFromResponseAsync<U>(HttpContent content, CancellationToken cancellationToken = default) where U : notnull
    {
        string mediaType = content.Headers.ContentType?.MediaType ?? string.Empty;
        string jsonContent = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(mediaType) || mediaType == this.jsonMediaType.MediaType)
        {
            try
            {
                return JsonSerializer.Deserialize<U>(jsonContent, SerializerOptions) ?? throw new DatasyncException("Invalid JSON content from server");
            }
            catch (JsonException ex)
            {
                throw new DatasyncException(ex.Message, ex);
            }
        }
        
        throw new DatasyncException("Invalid Media Type from server");
    }
}
