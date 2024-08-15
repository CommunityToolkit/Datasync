// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The options to use for offline operations.
/// </summary>
internal class DatasyncOfflineOptions(HttpClient client, Uri endpoint)
{
    /// <summary>
    /// The <see cref="HttpClient"/> to use for this request.
    /// </summary>
    public HttpClient HttpClient { get; } = client;

    /// <summary>
    /// The relative or absolute URI to the endpoint for this request.
    /// </summary>
    public Uri Endpoint { get; } = endpoint;
}
