// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

public interface IDatasyncHttpClientOptions
{
    /// <summary>
    /// The base address for the datasync service.
    /// </summary>
    Uri BaseAddress { get; }

    /// <summary>
    /// The HTTP pipeline to use.  It must be an ordered list of <see cref="DelegatingHandler"/> objects, potentially followed by a <see cref="HttpClientHandler"/> to use as a transport.
    /// </summary>
    IEnumerable<HttpMessageHandler> HttpPipeline { get; }

    /// <summary>
    /// A set of additional request headers to include in each HTTP request.
    /// </summary>
    IDictionary<string, string> HttpRequestHeaders { get; }

    /// <summary>
    /// The timeout to use with <see cref="HttpClient"/> instances.
    /// </summary>
    TimeSpan HttpTimeout { get; }
}
