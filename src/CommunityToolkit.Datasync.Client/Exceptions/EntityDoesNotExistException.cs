// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An exception to indicate that the entity requested does not exist.
/// </summary>
/// <remarks>
/// Creates a new <see cref="EntityDoesNotExistException"/> based on the given <see cref="ServiceResponse"/>
/// </remarks>
/// <param name="serviceResponse">The service response to use in constructing the exception.</param>
/// <param name="endpoint">The endpoint that was used to request the entity that does not exist.</param>
/// <param name="id">The ID of the entity that does not exist.</param>
public class EntityDoesNotExistException(ServiceResponse serviceResponse, Uri endpoint, string id) : DatasyncHttpException(serviceResponse)
{
    /// <summary>
    /// The endpoint of the table that was used for the entity.
    /// </summary>
    public Uri Endpoint { get; set; } = endpoint;

    /// <summary>
    /// The ID of the entity that is missing.
    /// </summary>
    public string Id { get; set; } = id;
}
