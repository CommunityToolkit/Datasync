// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The builder that is used to generate a map of the entities and other associated
/// information for the synchronization context.
/// </summary>
public class DatasyncSynchronizationContextBuilder
{
    /// <summary>
    /// When set, the <see cref="IHttpClientFactory"/> to use for <see cref="HttpClient"/> generation.
    /// </summary>
    private IHttpClientFactory? httpClientFactory;

    /// <summary>
    /// When set, the <see cref="HttpClient"/> to use for all connections.
    /// </summary>
    private HttpClient? httpClient;

    /// <summary>
    /// When set, the <see cref="Uri"/> mapping to the datasync service endpoint.
    /// </summary>
    private Uri? endpoint;

    /// <summary>
    /// <c>true</c> if a HttpClient generator has already been set.
    /// </summary>
    public bool HasHttpClientGenerator { get => this.httpClientFactory != null || this.httpClient != null || this.endpoint != null; }

    /// <summary>
    /// Sets the HttpClient generator to be an <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> to use for generating clients.</param>
    /// <returns>This object for chaining.</returns>
    /// <exception cref="DatasyncException">If the HttpClient generator is already set.</exception>
    public DatasyncSynchronizationContextBuilder SetHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
        ThrowIfGeneratorSet();
        this.httpClientFactory = httpClientFactory;
        return this;
    }

    /// <summary>
    /// Sets the HttpClient generator to be a specific <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use as the client.</param>
    /// <returns>This object for chaining.</returns>
    /// <exception cref="DatasyncException">If the HttpClient generator is already set.</exception>
    public DatasyncSynchronizationContextBuilder SetHttpClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ThrowIfGeneratorSet();
        this.httpClient = httpClient;
        return this;
    }

    /// <summary>
    /// Sets the HttpClient generator to produce HttpClients that connect to a specific endpoint.
    /// </summary>
    /// <param name="endpoint">The datasync service endpoint.</param>
    /// <returns>This object for chaining.</returns>
    /// <exception cref="DatasyncException">If the HttpClient generator is already set.</exception>
    /// <exception cref="UriFormatException">If the endpoint is not a valid endpoint for datasync operations.</exception>
    public DatasyncSynchronizationContextBuilder SetEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));
        ThrowIf.IsNotValidEndpoint(endpoint, nameof(endpoint));
        ThrowIfGeneratorSet();
        this.endpoint = endpoint;
        return this;
    }

    /// <summary>
    /// Throws a <see cref="DatasyncException"/> if the HttpClient generator is already set.
    /// </summary>
    /// <exception cref="DatasyncException">Thrown if the HttpClient generator is already set.</exception>
    private void ThrowIfGeneratorSet()
    {
        if (HasHttpClientGenerator)
        {
            throw new DatasyncException(ServiceErrorMessages.HttpClientGeneratorAlreadySet);
        }
    }
}
