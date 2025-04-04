// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// The possible results of a conflict resolution.
/// </summary>
public enum ConflictResolutionResult
{
    /// <summary>
    /// The default resolution, which is to do nothing and re-queue the operation.
    /// </summary>
    Default,

    /// <summary>
    /// The provided client object should be used.  This results in a new "force" submission
    /// to the server to over-write the server entity.
    /// </summary>
    Client,

    /// <summary>
    /// The server object should be used.  This results in the client object being updated
    /// with whatever the server object was provided.
    /// </summary>
    Server
}

/// <summary>
/// The model class returned by a conflict resolver to indicate the resolution of the conflict.
/// </summary>
public class ConflictResolution
{
    /// <summary>
    /// The conflict resolution result.
    /// </summary>
    public ConflictResolutionResult Result { get; set; } = ConflictResolutionResult.Default;

    /// <summary>
    /// The entity, if required.
    /// </summary>
    public object? Entity { get; set; }
}
