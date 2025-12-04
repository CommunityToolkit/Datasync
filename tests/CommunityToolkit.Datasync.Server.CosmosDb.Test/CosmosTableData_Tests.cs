// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Server.CosmosDb;
using CommunityToolkit.Datasync.TestCommon;
using AwesomeAssertions;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
public class CosmosTableData_Tests
{
    [Theory, ClassData(typeof(ITableData_TestData))]
    public void CCosmosTableData_Equals(ITableData a, ITableData b, bool expected)
    {
        CCosmosTableData entity_a = a.ToTableEntity<CCosmosTableData>();
        CCosmosTableData entity_b = b.ToTableEntity<CCosmosTableData>();

        entity_a.Equals(entity_b).Should().Be(expected);
        entity_b.Equals(entity_a).Should().Be(expected);

        entity_a.Equals(null).Should().BeFalse();
        entity_b.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void CCosmosTableData_MetadataRoundtrips()
    {
        DateTimeOffset testTime = DateTimeOffset.Now;

        CCosmosTableData sut1 = new() { Id = "t1", Deleted = false, UpdatedAt = testTime, Version = [0x61, 0x62, 0x63, 0x64, 0x65] };
        sut1.Version.Should().BeEquivalentTo("abcde"u8.ToArray());
        sut1.UpdatedAt.Should().Be(testTime);
    }
}

[ExcludeFromCodeCoverage]
public class CCosmosTableData : CosmosTableData
{
}
