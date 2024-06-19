// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// A concrete implementation of the <see cref="IHttpClientOptions"/> interface.
/// </summary>
public class HttpClientOptions : IHttpClientOptions
{
    /// <inheritdoc />
    public IEnumerable<HttpMessageHandler> HttpPipeline { get; set; } = [];

    /// <inheritdoc />
    public TimeSpan? HttpTimeout { get; set; } = TimeSpan.FromSeconds(60);
}
