// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore;

/// <summary>
/// A version of the <see cref="BaseEntityTableData"/> that is compatible with
/// most of the Entity Framework Core drivers.
/// </summary>
[Index(nameof(UpdatedAt), nameof(Deleted))]
public class EntityTableData : BaseEntityTableData
{
    /// <inheritdoc />
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public override DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;

    /// <inheritdoc />
    [Timestamp]
    public override byte[] Version { get; set; } = [];
}
