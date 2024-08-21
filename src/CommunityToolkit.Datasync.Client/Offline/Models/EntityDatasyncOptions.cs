// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;

namespace CommunityToolkit.Datasync.Client.Offline.Models;

/// <summary>
/// The offline options for a single entity.
/// </summary>
internal class EntityDatasyncOptions
{
    /// <summary>
    /// The endpoint for the entity type.
    /// </summary>
    public required Uri Endpoint { get; init; }

    /// <summary>
    /// The <see cref="HttpClient"/> pre-configured for communicating with the datasync service.
    /// </summary>
    public required HttpClient HttpClient { get; init; }

    /// <summary>
    /// The query description to use - set to null for "all entities in the data set"
    /// </summary>
    internal required QueryDescription QueryDescription { get; init; }
}
