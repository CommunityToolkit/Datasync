// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// An implementation of the <see cref="IHttpClientFactory"/> that provides a
/// suitable client for use with the Datasync client.
/// </summary>
internal class DatasyncHttpClientFactory(IDatasyncHttpClientOptions options) : IHttpClientFactory
{
    /// <summary>
    /// A factory method for creating the default <see cref="HttpClientHandler"/>
    /// </summary>
    protected Func<HttpMessageHandler> DefaultHandlerFactory = GetDefaultHttpClientHandler;

    /// <summary>
    /// A cache for the <see cref="HttpClient" /> instances created by this factory.
    /// </summary>
    protected readonly ConcurrentDictionary<string, HttpClient> _clients = new();

    /// <inheritdoc />
    public HttpClient CreateClient(string name)
    {
        Ensure.That(Options.BaseAddress).IsValidDatasyncUri();

        if (this._clients.TryGetValue(name, out HttpClient? client))
        {
            return client;
        }

        HttpMessageHandler roothandler = CreatePipeline();
        HttpClient newclient = new(roothandler, disposeHandler: true) { BaseAddress = Options.BaseAddress, Timeout = Options.HttpTimeout };
        foreach (KeyValuePair<string, string> header in Options.HttpRequestHeaders)
        {
            newclient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        // Attempt to add the client to the cache.  Don't worry if it doesn't work, as we'll just return the new client.
        _ = this._clients.TryAdd(name, newclient);
        return newclient;
    }

    /// <summary>
    /// The options to use when creating the <see cref="HttpClient" /> instances.
    /// </summary>
    internal IDatasyncHttpClientOptions Options { get; } = options;

    /// <summary>
    /// Transform a list of <see cref="HttpMessageHandler"/> objects into a chain suitable for using as the pipeline of a <see cref="HttpClient"/>.
    /// </summary>
    /// <returns>The chained <see cref="HttpMessageHandler"/></returns>
    internal HttpMessageHandler CreatePipeline()
    {
        HttpMessageHandler pipeline = Options.HttpPipeline.LastOrDefault() ?? this.DefaultHandlerFactory();
        if (pipeline is DelegatingHandler lastPolicy && lastPolicy.InnerHandler is null)
        {
            lastPolicy.InnerHandler = this.DefaultHandlerFactory();
            pipeline = lastPolicy;
        }

        foreach (HttpMessageHandler handler in Options.HttpPipeline.Reverse().Skip(1))
        {
            if (handler is DelegatingHandler policy)
            {
                policy.InnerHandler = pipeline;
                pipeline = policy;
            }
            else
            {
                throw new InvalidOperationException("All message handlers except the last one must be 'DelegatingHandler' instances.  The last handler may be a 'HttpClientHandler' instance.");
            }
        }

        return pipeline;
    }

    /// <summary>
    /// Returns a default <see cref="HttpClientHandler"/> instance that supports automatic decompression.
    /// </summary>
    /// <returns></returns>
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
