// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore;

/// <summary>
/// A version of the <see cref="BaseEntityTableData"/> that updates both the
/// <c>UpdatedAt</c> and <c>Version</c> fields within the repository (instead
/// of relying on the database server to do it).
/// </summary>
public class RepositoryControlledEntityTableData : BaseEntityTableData
{
    /// <inheritdoc />
    [UpdatedByRepository]
    public override DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;

    /// <inheritdoc />
    [UpdatedByRepository]
    public override byte[] Version { get; set; } = [];
}
