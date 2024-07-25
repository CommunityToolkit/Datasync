// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// A default implementation of the <see cref="IHttpClientFactory"/> that
/// uses a creator to create named clients.
/// </summary>
/// <param name="clientCreator">The function to use in creating clients.</param>
internal class DatasyncHttpClientFactory(Func<HttpClient> clientCreator) : IHttpClientFactory
{
    /// <summary>
    /// The client map that maps a name to a HttpClient.
    /// </summary>
    private readonly Dictionary<string, HttpClient> clientMap = [];

    /// <inheritdoc />
    public HttpClient CreateClient(string name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        if (this.clientMap.TryGetValue(name, out HttpClient? client))
        {
            return client;
        }

        HttpClient createdClient = clientCreator.Invoke();
        _ = this.clientMap.TryAdd(name, createdClient);
        return createdClient;
    }
}
