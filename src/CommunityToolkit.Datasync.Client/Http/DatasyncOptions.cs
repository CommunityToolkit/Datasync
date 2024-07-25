// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// The base class for all operational options bundles.
/// </summary>
public class DatasyncOptions
{
}

/// <summary>
/// The options used for modifying add operations.
/// </summary>
public class DatasyncAddOptions : DatasyncOptions
{
}

/// <summary>
/// The options used for modifying get operations.
/// </summary>
public class DatasyncGetOptions : DatasyncOptions
{
}

/// <summary>
/// The options used for modifying query operations.
/// </summary>
public class DatasyncQueryOptions : DatasyncOptions
{
    /// <summary>
    /// If set, only request entities that have been updated since the provided date.  This is used
    /// by the <see cref="OfflineDbContext.PullAsync{TEntity}(CancellationToken)"/> methods to handle
    /// incremental synchronization.
    /// </summary>
    public DateTimeOffset? RetrieveEntitiesUpdatedSince { get; set; }
}

/// <summary>
/// The options used for modifying remove operations.
/// </summary>
public class DatasyncRemoveOptions : DatasyncOptions
{
    /// <summary>
    /// If non-empty, then the entity must have the required version.  If it doesn't, then a
    /// <see cref="ConflictException{TEntity}"/> is thrown.
    /// </summary>
    public string RequiredVersion { get; set; } = string.Empty;

    /// <summary>
    /// If <c>true</c>, an <see cref="EntityNotFoundException"/> will be thrown if the entity is missing.
    /// </summary>
    public bool ThrowWhenMissing { get; set; } = false;
}

/// <summary>
/// The options used for modifying update operations.
/// </summary>
public class DatasyncUpdateOptions : DatasyncOptions
{
    /// <summary>
    /// If <c>true</c>, then the version in the provided entity must match the version on the server.
    /// </summary>
    public bool Force { get; set; }
}

