// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline.Internal;

/// <summary>
/// A model class representing a single service pull operation.
/// </summary>
internal class ServicePullOperation
{
    /// <summary>
    /// The entity type being pulled.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// The <see cref="HttpClient"/> to use for communication.
    /// </summary>
    public required HttpClient Client { get; init; }

    /// <summary>
    /// The endpoint for the entity type.
    /// </summary>
    public required Uri Endpoint { get; init; }

    /// <summary>
    /// The query string to append to the endpoint.
    /// </summary>
    public required string QueryString { get; init; }
}
