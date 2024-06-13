// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;

namespace CommunityToolkit.Datasync.Server.InMemory.Test;

[ExcludeFromCodeCoverage]
public class InMemoryTableData_Tests
{
    [Theory, ClassData(typeof(ITableData_TestData))]
    public void Equals_Works(ITableData a, ITableData b, bool expected)
    {
        InMemoryTableData entity_a = a.ToTableEntity<InMemoryTableData>();
        InMemoryTableData entity_b = b.ToTableEntity<InMemoryTableData>();

        entity_a.Equals(entity_b).Should().Be(expected);
        entity_b.Equals(entity_a).Should().Be(expected);

        entity_a.Equals(null).Should().BeFalse();
        entity_b.Equals(null).Should().BeFalse();
    }
}