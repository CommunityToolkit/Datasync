// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// A simple implementation of the <see cref="IHttpClientFactory"/>.
/// </summary>
/// <param name="options">The options to use in creating a client.</param>
public class DatasyncHttpClientFactory(IDatasyncHttpOptions options) : IHttpClientFactory
{
    /// <summary>
    /// The cache of <see cref="HttpClient"/> objects.
    /// </summary>
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new();

    /// <summary>
    /// A factory method for creating the default <see cref="HttpClientHandler"/>.
    /// </summary>
    protected Func<HttpMessageHandler> DefaultHandlerFactory = GetDefaultHttpClientHandler;

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> based on the options provided.
    /// </summary>
    /// <param name="name">The name of the client to create.</param>
    /// <returns>The created client.</returns>
    public HttpClient CreateClient(string name)
    {
        name ??= "";
        if (this._clients.TryGetValue(name, out HttpClient client))
        {
            return client;
        }

        HttpMessageHandler rootHandler = CreatePipeline(Options.HttpPipeline ?? []);
        client = new HttpClient(rootHandler) { Timeout = Options.HttpTimeout };
        if (Options.HttpRequestHeaders is not null)
        {
            foreach (KeyValuePair<string, string> header in Options.HttpRequestHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // We don't really care if we fail to add the client since we'll just return the one we created
        _ = this._clients.TryAdd(name, client);
        return client;
    }

    internal IDatasyncHttpOptions Options { get; } = options;

    /// <summary>
    /// Transforms a list of <see cref="HttpMessageHandler"/> instances into a chain of handlers suitable for
    /// using as a pipeline in an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="handlers">The ordered list of <see cref="HttpMessageHandler"/> objects to transform.</param>
    /// <returns>The chained <see cref="HttpMessageHandler"/> objects.</returns>
    /// <exception cref="ArgumentException">Thrown if the ordered list is invalid.</exception>
    internal HttpMessageHandler CreatePipeline(IEnumerable<HttpMessageHandler> handlers)
    {
        HttpMessageHandler pipeline = handlers.LastOrDefault() ?? this.DefaultHandlerFactory();
        if (pipeline is DelegatingHandler lastPolicy && lastPolicy.InnerHandler is null)
        {
            lastPolicy.InnerHandler = this.DefaultHandlerFactory();
            pipeline = lastPolicy;
        }

        foreach (HttpMessageHandler handler in handlers.Reverse().Skip(1))
        {
            if (handler is DelegatingHandler policy)
            {
                policy.InnerHandler = pipeline;
                pipeline = policy;
            }
            else
            {
                throw new ArgumentException("All message handlers except the last one must be a 'DelegatingHandler'", nameof(handlers));
            }
        }

        return pipeline;
    }

    /// <summary>
    /// Returns a <see cref="HttpClientHandler"/> that support automatic decompression.
    /// </summary>
    /// <returns>A <see cref="HttpMessageHandler"/>.</returns>
    protected static HttpMessageHandler GetDefaultHttpClientHandler()
    {
        HttpClientHandler handler = new();
        if (handler.SupportsAutomaticDecompression)
        {
            handler.AutomaticDecompression = DecompressionMethods.All;
        }

        return handler;
    }
}
