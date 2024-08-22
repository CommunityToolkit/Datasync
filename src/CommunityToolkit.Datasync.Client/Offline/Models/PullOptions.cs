// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline.Models;

/// <summary>
/// The options used to configure a pull operation.
/// </summary>
public class PullOptions
{
    /// <summary>
    /// Internal testing - ensures that we always use a delta-token filter when
    /// pulling data.
    /// </summary>
    internal bool AlwaysPullWithDeltaToken { get; set; } = false;

    /// <summary>
    /// If set, this is used as a query ID.  The query ID is a key to store
    /// a delta-token.  If not set, the query ID will be generated from the
    /// table name and query string provided.
    /// </summary>
    public string QueryId { get; set; } = string.Empty;
}
