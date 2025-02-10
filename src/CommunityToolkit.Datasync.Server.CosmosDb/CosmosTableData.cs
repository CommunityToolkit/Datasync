// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server.CosmosDb;
/// <summary>
/// Base implementation of <see cref="ITableData"/> for entities that are stored in a CosmosDB collection.
/// </summary>
public abstract class CosmosTableData : ITableData
{
    /// <inheritdoc/>
    public virtual string Id { get; set; } = string.Empty;
    /// <inheritdoc/>
    public virtual bool Deleted { get; set; } = false;
    /// <inheritdoc/>
    public virtual DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;
    /// <inheritdoc/>
    public virtual byte[] Version
    {
        get => Encoding.UTF8.GetBytes(ETag);
        set => ETag = Encoding.UTF8.GetString(value);
    }
    /// <summary>
    /// The ETag value for the entity.
    /// </summary>
    [JsonPropertyName("_etag")]
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// Equality comparison for <see cref="ITableData"/> instances.
    /// </summary>

    public bool Equals(ITableData? other)
    {
        return other != null && Id == other.Id && Version.SequenceEqual(other.Version);
    }
}
