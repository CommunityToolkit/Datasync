// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;

namespace CommunityToolkit.Datasync.Client.Offline.Models;

/// <summary>
/// The representation of a single pull from a single entity.
/// </summary>
internal class PullRequest
{
    /// <summary>
    /// The type of the entity being pulled.
    /// </summary>
    internal required Type EntityType { get; init; }

    /// <summary>
    /// The query ID - if blank, one will be created.
    /// </summary>
    internal string QueryId { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="HttpClient"/> to use to communicate with the remote service.
    /// </summary>
    internal required HttpClient HttpClient { get; init; }

    /// <summary>
    /// The relative or absolute URI to the endpoint for the service.
    /// </summary>
    internal Uri Endpoint { get; set; } = new Uri("", UriKind.Relative);

    /// <summary>
    /// The query description, used to limit the entities returned.
    /// </summary>
    internal QueryDescription QueryDescription { get; set; } = new();
}

/// <summary>
/// A generic version of a single pull request for an entity.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class PullRequest<TEntity> where TEntity : class
{
    /// <summary>
    /// The type of the entity being pulled.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// The query ID - if blank, one will be created.
    /// </summary>
    public string QueryId { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="HttpClient"/> to use to communicate with the remote service.
    /// </summary>
    public required HttpClient HttpClient { get; init; }

    /// <summary>
    /// The relative or absolute URI to the endpoint for the service.
    /// </summary>
    public Uri Endpoint { get; set; } = new Uri("", UriKind.Relative);

    /// <summary>
    /// The query that will be executed on the remote service to identify entities to be fetched.
    /// </summary>
    public IDatasyncPullQuery<TEntity> Query { get; set; } = new DatasyncPullQuery<TEntity>();
}