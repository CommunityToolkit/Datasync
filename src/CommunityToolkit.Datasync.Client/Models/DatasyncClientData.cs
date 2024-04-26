// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A base class for defining a data transfer object for the datasync client.
/// </summary>
public abstract class DatasyncClientData
{
    /// <summary>
    /// The item globally unique ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// If set to true, the item is deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    /// <summary>
    /// The last time that the record was updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The opaque version string.  This changes when the
    /// item is updated.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
