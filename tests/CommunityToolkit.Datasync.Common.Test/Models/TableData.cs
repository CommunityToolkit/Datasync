// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;

namespace CommunityToolkit.Datasync.Common.Test;

/// <summary>
/// A concrete implementation of the <see cref="ITableData"/> interface for testing purposes.
/// </summary>
public class TableData : ITableData
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool Deleted { get; set; } = false;

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;

    /// <inheritdoc />
    public byte[] Version { get; set; } = Array.Empty<byte>();

    public bool Equals(ITableData other)
        => other is not null && Id == other.Id && Version.SequenceEqual(other.Version);
}
