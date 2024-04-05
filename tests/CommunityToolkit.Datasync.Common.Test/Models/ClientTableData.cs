// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;

namespace CommunityToolkit.Datasync.Common.Test.Models;

/// <summary>
/// The client version of the <see cref="TableData"/> class.
/// </summary>
[ExcludeFromCodeCoverage]
public class ClientTableData
{
    public ClientTableData()
    {
    }

    public ClientTableData(object source)
    {
        if (source is ITableData metadata)
        {
            Id = metadata.Id;
            UpdatedAt = metadata.UpdatedAt;
            Version = Convert.ToBase64String(metadata.Version);
            Deleted = metadata.Deleted;
        }
    }

    public string Id { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string Version { get; set; }
    public bool Deleted { get; set; }
}