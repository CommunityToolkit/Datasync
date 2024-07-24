// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The options to configure a pull operation for data synchronization.
/// </summary>
public class PullOperationOptions
{
    /// <summary>
    /// If set, this is used as a query ID.  The query ID is a key to store a delta-token.  If not set, the query ID 
    /// will be generated from the table name.
    /// </summary>
    public string QueryId { get; set; } = string.Empty;
}
