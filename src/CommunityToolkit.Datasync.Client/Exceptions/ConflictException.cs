// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An exception that represents a conflict between the client and server.
/// </summary>
/// <typeparam name="TEntity">The type of entity being transmitted to the server.</typeparam>
/// <remarks>
/// Creates a new <see cref="ConflictException{TEntity}"/> based on a client entity and server response.
/// </remarks>
/// <param name="clientEntity">The client entity that was sent to the server.</param>
/// <param name="serviceResponse">The response from the server.</param>
public class ConflictException<TEntity>(TEntity? clientEntity, ServiceResponse<TEntity> serviceResponse) : DatasyncHttpException(serviceResponse)
{
    /// <summary>
    /// The client entity that was sent to the server.
    /// </summary>
    public TEntity? ClientEntity { get; } = clientEntity;

    /// <summary>
    /// The entity that is stored on the server.
    /// </summary>
    public TEntity? ServerEntity { get; } = serviceResponse.Value;
}
