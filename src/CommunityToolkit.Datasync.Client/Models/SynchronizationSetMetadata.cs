// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Models;

/// <summary>
/// The set of metadata needed to track pull operations so that the next pull
/// operation picks up where the last one finished.
/// </summary>
public class SynchronizationSetMetadata
{
    /// <summary>
    /// The ID of the pull operation.
    /// </summary>
    /// <remarks>
    /// In most cases, the id will be the name of the DbSet property being synchronized.  However,
    /// in cases where multiple pull operations must be used to synchronize the data, you may have
    /// a named query.  The ID of the query is used to generate the synchronization ID.
    /// </remarks>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// When a query is executed, the delta-token is appended so we only pull entities with an
    /// UpdatedAt time greater than the delta-token.
    /// </summary>
    public DateTimeOffset DeltaToken { get; set; } = DateTimeOffset.MinValue;
}
