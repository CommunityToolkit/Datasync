// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore;

/// <summary>
/// The base class for all the Entity Framework Core based table data classes.
/// </summary>
public abstract class BaseEntityTableData : ITableData
{
    /// <inheritdoc />
    [Key]
    public virtual string Id { get; set; } = string.Empty;

    /// <inheritdoc />
    public virtual bool Deleted { get; set; } = false;

    /// <inheritdoc />
    public virtual DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;

    /// <inheritdoc />
    public virtual byte[] Version { get; set; } = Array.Empty<byte>();

    /// <inheritdoc />
    public bool Equals(ITableData? other)
        => other != null && Id == other.Id && Version.SequenceEqual(other.Version);
}
