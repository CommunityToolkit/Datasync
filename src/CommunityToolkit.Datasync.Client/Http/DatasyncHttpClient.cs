// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// An implementation of the <see cref="IDatasyncClient"/> that communicates with
/// a HTTP-based OData client.
/// </summary>
/// <typeparam name="TEntity">The type of entity being processed.</typeparam>
internal class DatasyncHttpClient<TEntity> : IDatasyncClient<TEntity> where TEntity : class
{
    /// <summary>
    /// Creates a new <see cref="DatasyncHttpClient{TEntity}"/> based on the endpoint, client, and serializer options.
    /// </summary>
    /// <param name="endpoint">The absolute URI for the endpoint handling the data.</param>
    /// <param name="client">The <see cref="HttpClient"/> to use for communication.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing content.</param>
    internal DatasyncHttpClient(Uri endpoint, HttpClient client, JsonSerializerOptions serializerOptions)
    {
        Endpoint = Ensure.That(endpoint, nameof(endpoint)).IsDatasyncEndpoint().Value;
        Client = Ensure.That(client, nameof(client)).IsNotNull().Value;
        SerializerOptions = Ensure.That(serializerOptions, nameof(serializerOptions)).IsNotNull().Value;
    }

    /// <summary>
    /// The absolute URI for the endpoint handling the data.
    /// </summary>
    internal Uri Endpoint { get; }

    /// <summary>
    /// The <see cref="HttpClient"/> to use for communication.
    /// </summary>
    internal HttpClient Client { get; }

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing content.
    /// </summary>
    internal JsonSerializerOptions SerializerOptions { get; }
}
