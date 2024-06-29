// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0290 // Use primary constructor

using CommunityToolkit.Datasync.Client.Exceptions;
using CommunityToolkit.Datasync.Client.Models;
using CommunityToolkit.Datasync.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;

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
    /// Creates a new <see cref="RemoteDataset{T}"/> object using a specific client, options, and path.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use for accessing the remote dataset.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of JSON content.</param>
    /// <param name="path">The relative path to the table endpoint.</param>
    public RemoteDataset(HttpClient client, JsonSerializerOptions options, string path)
    {
        Client = Ensure.That(client, nameof(client)).IsNotNull().And.HasDatasyncEndpoint().Value;
        SerializerOptions = Ensure.That(options, nameof(options)).IsNotNull().Value;
        DatasetPath = Ensure.That(path, nameof(path)).IsNotNullOrWhiteSpace().And.IsHttpPath().Value;
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
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
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
    public ValueTask<long> CountAsync(string query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<T> GetAsync(string id, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<Page<T>> GetPageAsync(string pathAndQuery, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RemoveAsync(string id, string? ifMatchVersion, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<T> ReplaceAsync(T entity, bool force, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    #endregion

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
            return JsonSerializer.Deserialize<U>(jsonContent, SerializerOptions) ?? throw new JsonException("Invalid JSON content from server");
        }
        
        throw new DatasyncException("Invalid Media Type from server");
    }
}
