// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// An interface for describing the options required for the Datasync HTTP client factory.
/// </summary>
public interface IDatasyncHttpClientOptions
{
    /// <summary>
    /// The HTTP Pipeline to use.  This can be null.  If set, it must
    /// be an ordered set of <see cref="DelegatingHandler"/> objects,
    /// potentially followed by a <see cref="HttpClientHandler"/> for
    /// a transport.
    /// </summary>
    IEnumerable<HttpMessageHandler> HttpPipeline { get; }

    /// <summary>
    /// The timeout to use with <see cref="HttpClient"/> connections.
    /// </summary>
    TimeSpan HttpTimeout { get; }
}
