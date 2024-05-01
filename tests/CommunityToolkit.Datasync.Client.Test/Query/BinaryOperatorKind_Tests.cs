// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using CommunityToolkit.Datasync.Client.Query.OData;

namespace CommunityToolkit.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class BinaryOperatorKind_Tests
{
    [Theory]
    [InlineData(0, "or")]
    [InlineData(1, "and")]
    [InlineData(2, "eq")]
    [InlineData(3, "ne")]
    [InlineData(4, "gt")]
    [InlineData(5, "ge")]
    [InlineData(6, "lt")]
    [InlineData(7, "le")]
    [InlineData(8, "add")]
    [InlineData(9, "sub")]
    [InlineData(10, "mul")]
    [InlineData(11, "div")]
    [InlineData(12, "mod")]
    public void ToODataString_Valid(int intKind, string expected)
    {
        BinaryOperatorKind kind = (BinaryOperatorKind)intKind;
        kind.ToODataString().Should().Be(expected);
    }

    [Fact]
    public void ToODataString_Invalid_Throws()
    {
        const BinaryOperatorKind kind = (BinaryOperatorKind)101;
        Assert.ThrowsAny<Exception>(() => kind.ToODataString());
    }
}
