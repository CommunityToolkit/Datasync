// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// Creates a new <see cref="IHttpClientFactory"/> that builds clients on an as-needed
/// basis.
/// </summary>
/// <param name="options">The options to use for creating the <see cref="HttpClient"/> instances.</param>
public class HttpClientFactory(HttpClientOptions options) : IHttpClientFactory
{
    /// <summary>
    /// The cache of clients available to the client factory.
    /// </summary>
    private readonly Dictionary<string, HttpClient> _clientCache = [];

    /// <summary>
    /// Creates and configures an HttpClient instance using the configuration that corresponds to the logical name 
    /// specified by <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The logical name of the client.</param>
    /// <returns>A configured <see cref="HttpClient"/> for the service.</returns>
    public HttpClient CreateClient(string name)
    {
        if (this._clientCache.TryGetValue(name, out HttpClient? client))
        {
            return client;
        }

        client = GenerateClient();
        _ = this._clientCache.TryAdd(name, client);
        return client;
    }

    /// <summary>
    /// Generates a new <see cref="HttpClient"/> based on the options in the <see cref="HttpClientOptions"/>
    /// passed to the factory.
    /// </summary>
    /// <returns>A valid <see cref="HttpClient"/> instance.</returns>
    internal HttpClient GenerateClient()
    {
        HttpMessageHandler rootHandler = CreatePipeline(options.HttpPipeline);
        HttpClient client = new(rootHandler)
        {
            BaseAddress = IsHttpEndpoint(options.Endpoint) ? options.Endpoint : null,
            Timeout = options.Timeout
        };

        client.AddHeaderIfNotNullOrWhiteSpace(DatasyncHttpHeaders.UserAgent, options.UserAgent);

        // Support for Azure Mobile Apps services.
#pragma warning disable CS0618 // Type or member is obsolete
        client.AddHeaderIfNotNullOrWhiteSpace(DatasyncHttpHeaders.InstallationId, options.InstallationId);
        client.AddHeaderIfNotNullOrWhiteSpace(DatasyncHttpHeaders.ProtocolVersion, options.ProtocolVersion);
#pragma warning restore CS0618 // Type or member is obsolete

        return client;
    }

    /// <summary>
    /// Transform a list of <see cref="HttpMessageHandler"/> objects into a chain suitable for using
    /// as the pipeline of a <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="handlers">The list of <see cref="HttpMessageHandler"/> objects to transform</param>
    /// <returns>The chained <see cref="HttpMessageHandler"/></returns>
    internal static HttpMessageHandler CreatePipeline(IEnumerable<HttpMessageHandler> handlers)
    {
        HttpMessageHandler pipeline = handlers.LastOrDefault() ?? GetDefaultHttpClientHandler();
        if (pipeline is DelegatingHandler lastPolicy && lastPolicy.InnerHandler == null)
        {
            lastPolicy.InnerHandler = GetDefaultHttpClientHandler();
            pipeline = lastPolicy;
        }

        // Wire handlers up in reverse order
        foreach (HttpMessageHandler handler in handlers.Reverse().Skip(1))
        {
            if (handler is DelegatingHandler policy)
            {
                policy.InnerHandler = pipeline;
                pipeline = policy;
            }
            else
            {
                throw new ArgumentException("All message handlers except the last one must be 'DelegatingHandler'", nameof(handlers));
            }
        }

        return pipeline;
    }

    /// <summary>
    /// Returns a <see cref="HttpClientHandler"/> that supports automatic decompression.
    /// </summary>
    internal static HttpMessageHandler GetDefaultHttpClientHandler()
    {
        HttpClientHandler handler = new();
        handler.SetAutomaticDecompression(handler.SupportsAutomaticDecompression);
        return handler;
    }

    /// <summary>
    /// Returns true if the endpoint is a HTTP endpoint.
    /// </summary>
    /// <param name="uri">The Uri to check.</param>
    /// <returns><c>true</c> if the scheme is HTTP; <c>false</c> otherwise.</returns>
    internal static bool IsHttpEndpoint(Uri uri)
        => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
}
