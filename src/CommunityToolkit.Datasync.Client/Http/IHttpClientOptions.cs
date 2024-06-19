// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// The options required for configuring a new <see cref="HttpClient"/> for
/// use with the Datasync version of the <see cref="IHttpClientFactory"/>.
/// </summary>
public interface IHttpClientOptions
{
    /// <summary>
    /// The HTTP Pipeline to use.  This can be null.  If set, it must
    /// be an ordered set of <see cref="DelegatingHandler"/> objects,
    /// potentially followed by a <see cref="HttpClientHandler"/> for
    /// a transport.
    /// </summary>
    IEnumerable<HttpMessageHandler> HttpPipeline { get; }

    /// <summary>
    /// If set, the timeout to use with <see cref="HttpClient"/> connections.
    /// If not set, the default of 100,000ms (100 seconds) will be used.
    /// </summary>
    TimeSpan? HttpTimeout { get; }
}
