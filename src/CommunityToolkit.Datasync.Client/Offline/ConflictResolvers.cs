// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// An abstract class that provides a mechanism for resolving conflicts between client and server objects of a specified
/// type asynchronously.  The object edition of the conflict resolver just calls the typed version.
/// </summary>
/// <typeparam name="TEntity">The type of entity being resolved.</typeparam>
public abstract class AbstractConflictResolver<TEntity> : IConflictResolver<TEntity>
{
    /// <inheritdoc />
    public abstract Task<ConflictResolution> ResolveConflictAsync(TEntity? clientObject, TEntity? serverObject, CancellationToken cancellationToken = default);

    /// <summary>
    /// The object version of the resolver calls the typed version.
    /// </summary>
    /// <param name="clientObject"></param>
    /// <param name="serverObject"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<ConflictResolution> ResolveConflictAsync(object? clientObject, object? serverObject, CancellationToken cancellationToken = default)
        => await ResolveConflictAsync((TEntity?)clientObject, (TEntity?)serverObject, cancellationToken);
}

/// <summary>
/// A conflict resolver where the client object always wins.
/// </summary>
public class ClientWinsConflictResolver : IConflictResolver
{
    /// <inheritdoc />
    public Task<ConflictResolution> ResolveConflictAsync(object? clientObject, object? serverObject, CancellationToken cancellationToken = default)
        => Task.FromResult(new ConflictResolution
        {
            Result = ConflictResolutionResult.Client,
            Entity = clientObject
        });
}

/// <summary>
/// A conflict resolver where the server object always wins.
/// </summary>
public class ServerWinsConflictResolver : IConflictResolver
{
    /// <inheritdoc />
    public Task<ConflictResolution> ResolveConflictAsync(object? clientObject, object? serverObject, CancellationToken cancellationToken = default)
        => Task.FromResult(new ConflictResolution
        {
            Result = ConflictResolutionResult.Server,
            Entity = serverObject
        });
}

