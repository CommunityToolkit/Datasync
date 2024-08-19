// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
///  A mapping for the delta-token store, which records the last sequence number of
///  the entity that was synchronized (normally a time stamp).
/// </summary>
public class DatasyncDeltaToken
{
    /// <summary>
    /// The ID of the entity type.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// The value of the sequence number.
    /// </summary>
    public required long Sequence { get; set; }
}
