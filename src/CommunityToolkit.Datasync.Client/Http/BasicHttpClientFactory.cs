// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// A <see cref="IHttpClientFactory"/> implementation that always
/// returns the same <see cref="HttpClient"/>.
/// </summary>
/// <param name="client">The <see cref="HttpClient"/> to use</param>
internal class BasicHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    /// <inheritdoc />
    public HttpClient CreateClient(string name) => client;
}
