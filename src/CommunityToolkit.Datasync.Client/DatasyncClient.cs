// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A client that provides access to a datasync service.
/// </summary>
public class DatasyncClient : IDisposable
{
    /// <summary>
    /// This is for unit testing only.
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected DatasyncClient()
    {
        ClientOptions = new DatasyncClientOptions();
        ServiceOptions = new DatasyncServiceOptions();
        Endpoint = new Uri("http://localhost/");
        HttpClientFactory = new DefaultHttpClientFactory(Endpoint, new HttpClientOptions());
    }

    /// <summary>
    /// Creates a new <see cref="DatasyncClient"/> that connects to the specified endpoint for datasync operations.
    /// </summary>
    /// <param name="endpoint">The endpoint of the datasync service.</param>
    /// <exception cref="UriFormatException">if the endpoint is not a valid datasync Uri.</exception>
    public DatasyncClient(string endpoint) : this(new Uri(endpoint, UriKind.Absolute), new DatasyncClientOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="DatasyncClient"/> that connects to the specified endpoint for datasync operations.
    /// </summary>
    /// <param name="endpoint">The endpoint of the datasync service.</param>
    /// <exception cref="UriFormatException">if the endpoint is not a valid datasync Uri.</exception>
    public DatasyncClient(Uri endpoint) : this(endpoint, new DatasyncClientOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="DatasyncClient"/> that connects to the specified endpoint for datasync operations.
    /// </summary>
    /// <param name="endpoint">The endpoint of the datasync service.</param>
    /// <param name="options">The options to use for handling the connection to the datasync service.</param>
    /// <exception cref="UriFormatException">if the endpoint is not a valid datasync Uri.</exception>
    public DatasyncClient(string endpoint, DatasyncClientOptions options) : this(new Uri(endpoint, UriKind.Absolute), options)
    {
    }

    /// <summary>
    /// Creates a new <see cref="DatasyncClient"/> that connects to the specified endpoint for datasync operations.
    /// </summary>
    /// <param name="endpoint">The endpoint of the datasync service.</param>
    /// <param name="options">The options to use for handling the connection to the datasync service.</param>
    /// <exception cref="UriFormatException">if the endpoint is not a valid datasync Uri.</exception>
    public DatasyncClient(Uri endpoint, DatasyncClientOptions options)
    {
        Endpoint = NormalizeEndpoint(Ensure.That(endpoint, nameof(endpoint)).IsNotNull().And.IsDatasyncEndpoint().Value);
        ClientOptions = Ensure.That(options, nameof(options)).IsNotNull().Value;
        ServiceOptions = options.DatasyncServiceOptions;
        HttpClientFactory = options.HttpClientFactory ?? new DefaultHttpClientFactory(Endpoint, new HttpClientOptions());
    }

    /// <summary>
    /// The client options for communicating with the datasync service.
    /// </summary>
    public DatasyncClientOptions ClientOptions { get; }

    /// <summary>
    /// Absolute URI of the datasync service.
    /// </summary>
    public Uri Endpoint { get; }

    /// <summary>
    /// The HTTP client factory to use to retrieve a <see cref="HttpClient"/> for
    /// communicating with the datasync service.
    /// </summary>
    public IHttpClientFactory HttpClientFactory { get; }

    /// <summary>
    /// The service options (for serializer / shared settings).
    /// </summary>
    public IDatasyncServiceOptions ServiceOptions { get; }

    #region IDisposable
    /// <summary>
    /// Implementation of the <see cref="IDisposable"/> pattern for disposing
    /// this client.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Normalizes an endpoint by removing any query and fragment, then ensuring that the
    /// path has a trailing slash.
    /// </summary>
    /// <param name="endpoint">The endpoint to normalizer.</param>
    /// <returns>The normalized endpoint.</returns>
    internal static Uri NormalizeEndpoint(Uri endpoint)
    {
        UriBuilder builder = new(endpoint) { Query = string.Empty, Fragment = string.Empty };
        builder.Path = builder.Path.TrimEnd('/') + "/";
        return builder.Uri;
    }

    /// <summary>
    /// Implementation of the <see cref="IDisposable"/> pattern for disposing
    /// this client.
    /// </summary>
    /// <param name="disposing">Indicates if being called from the Dispose() method or the finalizer.</param>
    [ExcludeFromCodeCoverage]
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (HttpClientFactory is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
    #endregion
}
