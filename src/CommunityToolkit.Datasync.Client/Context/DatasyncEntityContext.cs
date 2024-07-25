// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The entity context stores data associated with the entity that is used
/// for communicating with the remote datasync service.
/// </summary>
/// <param name="relativeUri">The relative URI of the entity.</param>
public class DatasyncEntityContext(string relativeUri)
{
    /// <summary>
    /// The (normally relative) URI to the endpoint for this entity.
    /// </summary>
    internal Uri Endpoint { get; set; } = new Uri(relativeUri);

    /// <summary>
    /// The name of the client to use when communicating with the service.
    /// </summary>
    internal string HttpClientName { get; set; } = string.Empty;
}
