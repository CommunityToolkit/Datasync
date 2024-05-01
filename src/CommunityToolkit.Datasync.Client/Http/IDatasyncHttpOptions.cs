// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// The options for configuring the HTTP client used by the <see cref="RemoteDataset{T}"/>.
/// </summary>
public interface IDatasyncHttpOptions
{
    /// <summary>
    /// An ordered set of <see cref="HttpMessageHandler"/> instances that will be used to set up a pipeline for modifying the
    /// HTTP request and response.  This is used to set up the default <see cref="HttpClient"/> or <see cref="IHttpClientFactory"/>.
    /// </summary>
    IEnumerable<HttpMessageHandler> HttpPipeline { get; }

    /// <summary>
    /// A set of headers that will be added to the HTTP request.  This is used to set up the default <see cref="HttpClient"/>
    /// </summary>
    IDictionary<string, string> HttpRequestHeaders { get; }

    /// <summary>
    /// The default timeout to use for HTTP requests.  If not set, the default timeout is 100 seconds.
    /// </summary>
    TimeSpan HttpTimeout { get; }
}
