// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// Definition of a conflict resolver.  This is used in push situations where
/// the server returns a 409 or 412 status code indicating that the client is
/// out of step with the server.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Resolves the conflict between two objects - client side and server side.
    /// </summary>
    /// <param name="clientObject">The client object.</param>
    /// <param name="serverObject">The server object.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The conflict resolution.</returns>
    Task<ConflictResolution> ResolveConflictAsync(object? clientObject, object? serverObject, CancellationToken cancellationToken = default);
}

/// <summary>
/// Definition of a conflict resolver.  This is used in push situations where
/// the server returns a 409 or 412 status code indicating that the client is
/// out of step with the server.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public interface IConflictResolver<TEntity> : IConflictResolver
{
    /// <summary>
    /// Resolves the conflict between two objects - client side and server side.
    /// </summary>
    /// <param name="clientObject">The client object.</param>
    /// <param name="serverObject">The server object.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The conflict resolution.</returns>
    Task<ConflictResolution> ResolveConflictAsync(TEntity? clientObject, TEntity? serverObject, CancellationToken cancellationToken = default);
}
