// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline.Internal;

/// <summary>
/// The options to use for offline operations.
/// </summary>
internal class OfflineOptions()
{
    /// <summary>
    /// The <see cref="HttpClient"/> to use for this request.
    /// </summary>
    public required IHttpClientFactory HttpClientFactory { get; init; }

    /// <summary>
    /// The list of entity client names.
    /// </summary>
    internal Dictionary<Type, string> EntityClientNames { get; set; } = [];

    /// <summary>
    /// The list of entity endpoints.
    /// </summary>
    internal Dictionary<Type, Uri> EntityEndpoints { get; set; } = [];

    /// <summary>
    /// Adds an entity to the mapping of options.
    /// </summary>
    /// <param name="entityType">The entity type to store.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="endpoint">The endpoint serving the datasync services.</param>
    public void AddEntity(Type entityType, string clientName, Uri endpoint)
    {
        EntityClientNames.Add(entityType, clientName);
        EntityEndpoints.Add(entityType, endpoint);
    }

    /// <summary>
    /// Returns a <see cref="HttpClient"/> for communicating with the remote service.
    /// </summary>
    /// <param name="entityType">The entity type being synchronized.</param>
    /// <returns>The <see cref="HttpClient"/> to use for communication.</returns>
    public HttpClient GetClient(Type entityType)
        => HttpClientFactory.CreateClient(EntityClientNames[entityType].AsNullableEmptyString());

    /// <summary>
    /// Returns a <see cref="Uri"/> for communicating with the remote service.
    /// </summary>
    /// <param name="entityType">The entity type being synchronized.</param>
    /// <returns>The <see cref="Uri"/> to use for synchronization operations.</returns>
    public Uri GetEndpoint(Type entityType)
        => EntityEndpoints.TryGetValue(entityType, out Uri? endpoint) ? endpoint : new Uri("", UriKind.Relative);
}
