// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LiteDB;

namespace CommunityToolkit.Datasync.Server.LiteDb;

/// <summary>
/// An implementation of the <see cref="ITableData"/> interface for
/// handling entities in a LiteDB database.
/// </summary>
public class LiteDbTableData : ITableData
{
    /// <inheritdoc />
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool Deleted { get; set; } = false;

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;

    /// <inheritdoc />
    public byte[] Version { get; set; } = [];

    /// <inheritdoc />
    public bool Equals(ITableData? other)
        => other != null && Id == other.Id && Version.SequenceEqual(other.Version);
}
