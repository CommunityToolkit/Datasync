// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon.Models;
using Xunit;

namespace CommunityToolkit.Datasync.Common.Test;

[ExcludeFromCodeCoverage]
public class ITableData_TestData : TheoryData<TableData, TableData, bool>
{
    private readonly string[] ids =
    [
        "",
        "t1",
        Guid.NewGuid().ToString()
    ];

    private readonly DateTimeOffset[] dates =
    [
        DateTimeOffset.MinValue,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.Now.AddDays(-1),
        DateTimeOffset.UtcNow,
        DateTimeOffset.MaxValue
    ];

    // Note: PgEntityTableData uses a uint32 for the RowVersion, which is decoded
    // into the Version byte array.  This means that, for testing purposes, we can
    // only test 4-byte arrays with certainty.
    private readonly byte[][] versions =
    [
        [],
        [0x01, 0x02, 0x03, 0x04],
        [0x60, 0x50, 0x40, 0x30]
    ];

    public ITableData_TestData()
    {
        List<TableData> sourceMaterial = [];
        foreach (string id in this.ids)
        {
            foreach (DateTimeOffset date in this.dates)
            {
                foreach (byte[] version in this.versions)
                {
                    sourceMaterial.Add(new TableData { Id = id, Deleted = false, UpdatedAt = date, Version = version.ToArray() });
                    sourceMaterial.Add(new TableData { Id = id, Deleted = true, UpdatedAt = date, Version = version.ToArray() });
                }
            }
        }

        for (int a = 0; a < sourceMaterial.Count; a++)
        {
            for (int b = 0; b < sourceMaterial.Count; b++)
            {
                Add(sourceMaterial[a], sourceMaterial[b], sourceMaterial[a].Id == sourceMaterial[b].Id && sourceMaterial[a].Version.SequenceEqual(sourceMaterial[b].Version));
            }
        }
    }
}
