// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using CommunityToolkit.Datasync.Client.Query.OData;

namespace CommunityToolkit.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class ConstantNode_Tests
{
    [Theory]
    [InlineData(null, "null")]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    [InlineData((byte)42, "2A")]
    [InlineData((int)42, "42")]
    [InlineData((uint)42, "42L")]
    [InlineData((short)42, "42")]
    [InlineData('a', "'a'")]
    [InlineData('\'', "''''")]
    [InlineData(42.2f, "42.2f")]
    [InlineData(4.2E14, "420000000000000.0")]
    [InlineData((double)42.4, "42.4")]
    [InlineData((double)42, "42.0")]
    [InlineData((long)4000, "4000L")]
    public void ToODataString_ConstantNode(object sut, string expected)
    {
        ConstantNode node = new(sut);
        Assert.Equal(expected, node.ToODataString());
    }

    [Fact]
    public void ToODataString_ConstantNode_Decimal()
    {
        ConstantNode node = new(5.8M);
        Assert.Equal("5.8M", node.ToODataString());
    }
}
