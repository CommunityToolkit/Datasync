// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// An implementation of the <see cref="IHttpClientFactory"/> that is used
/// for systems that don't have a default implementation of the factory.
/// </summary>
/// <param name="endpoint">The base endpoint.</param>
/// <param name="options">The options to use in creating new <see cref="HttpClient"/> objects.</param>
public class DefaultHttpClientFactory(Uri endpoint, IHttpClientOptions options) : IHttpClientFactory
{
    /// <summary>
    /// The base endpoint for the <see cref="HttpClient"/> that is produced.
    /// </summary>
    internal Uri Endpoint { get; } = endpoint;

    /// <summary>
    /// The options to use in creating new <see cref="HttpClient"/> objects.
    /// </summary>
    internal IHttpClientOptions Options { get; } = options;

    /// <inheritdoc />
    public HttpClient CreateClient(string name)
    {
        throw new NotImplementedException();
    }
}
