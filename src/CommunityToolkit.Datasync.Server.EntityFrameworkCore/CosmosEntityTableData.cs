// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore;

/// <summary>
/// A version of the <see cref="BaseEntityTableData"/> that is compatible with the
/// requirements of Cosmos.
/// </summary>
public class CosmosEntityTableData : BaseEntityTableData
{
    /// <inheritdoc />
    [UpdatedByRepository]
    public override DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;

    /// <inheritdoc />
    [NotMapped]
    public override byte[] Version
    {
        get => Encoding.UTF8.GetBytes(EntityTag);
        set => EntityTag = Encoding.UTF8.GetString(value);
    }

    /// <summary>
    /// The entity tag for the Cosmos entity.
    /// </summary>
    [Timestamp, JsonIgnore]
    public string EntityTag { get; set; } = string.Empty;
}
