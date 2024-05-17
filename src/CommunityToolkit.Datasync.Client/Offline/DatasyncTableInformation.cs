// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// To support incremental sync, every synchronization event is recorded in the <see cref="OfflineDbContext"/>
/// together with the last time that the synchronization happened.  This information is used to determine what
/// parameters to add to the query to get only the changes since the last sync.
/// </summary>
[Table("__datasync_tableinfo")]
public sealed class DatasyncTableInformation
{
    /// <summary>
    /// The unique ID for the query.  This is generated from the table name
    /// and query when not provided.
    /// </summary>
    [Key]
    public string QueryId { get; set; } = string.Empty;

    /// <summary>
    /// The number of ticks when the last synchronization happened (or 0L if never synchronized).
    /// </summary>
    [Required]
    public long LastSynchronization { get; set; } = 0L;
}
