// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// An implementation of the <see cref="IHttpClientFactory"/> that is used
/// for systems that don't have a default implementation of the factory.
/// </summary>
/// <param name="endpoint">The base endpoint.</param>
/// <param name="options">The options to use in creating new <see cref="HttpClient"/> objects.</param>
public class DefaultHttpClientFactory(Uri endpoint, IHttpClientOptions options) : IHttpClientFactory
{
    private readonly object _lock = new();

    /// <summary>
    /// The base endpoint for the <see cref="HttpClient"/> that is produced.
    /// </summary>
    internal Uri Endpoint { get; } = endpoint;

    /// <summary>
    /// The options to use in creating new <see cref="HttpClient"/> objects.
    /// </summary>
    internal IHttpClientOptions Options { get; } = options;

    /// <summary>
    /// A cache of all the <see cref="HttpClient"/> objects that have been given out
    /// within the application.
    /// </summary>
    internal ConcurrentDictionary<string, HttpClient> _cache = new();

    /// <inheritdoc />
    public HttpClient CreateClient(string name)
    {
        lock (this._lock)
        {
            if (this._cache.TryGetValue(name, out HttpClient? client))
            {
                return client;
            }

            HttpMessageHandler rootHandler = CreatePipeline(Options.HttpPipeline);
            HttpClient createdClient = new(rootHandler)
            {
                BaseAddress = Endpoint,
                Timeout = Options.HttpTimeout
            };
            _ = this._cache.TryAdd(name, createdClient);
            return createdClient;
        }
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
        if (handler.SupportsAutomaticDecompression)
        {
            handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip;
        }

        return handler;
    }
}
