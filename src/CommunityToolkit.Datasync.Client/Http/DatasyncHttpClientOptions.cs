// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// The default values for the <see cref="IDatasyncHttpClientOptions"/> interface.
/// </summary>
public class DatasyncHttpClientOptions : IDatasyncHttpClientOptions
{
    /// <summary>
    /// The base address for the datasync service.
    /// </summary>
    public Uri BaseAddress { get; set; } = new("http://localhost/");

    /// <summary>
    /// The HTTP pipeline to use.  It must be an ordered list of <see cref="DelegatingHandler"/> objects, potentially followed by a <see cref="HttpClientHandler"/> to use as a transport.
    /// </summary>
    /// <remarks>
    /// There is no default pipeline.  If no pipeline is provided, the default
    /// client handler (which allows for automatic decompression) will be used.
    /// </remarks>
    public IEnumerable<HttpMessageHandler> HttpPipeline { get; set; } = [];

    /// <summary>
    /// A set of additional request headers to include in each HTTP request.
    /// </summary>
    /// <remarks>
    /// There are no default additional HTTP request headers.
    /// </remarks>
    public IDictionary<string, string> HttpRequestHeaders { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The timeout to use with <see cref="HttpClient"/> instances.
    /// </summary>
    /// <remarks>
    /// The default timeout is 30 seconds.
    /// </remarks>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
