// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;
using System.Drawing;

namespace CommunityToolkit.Datasync.Client.Test.Query.OData;

[ExcludeFromCodeCoverage]
public class TableLookup_Tests
{
    [Fact]
    public void ToODataString_ConstantNode_Null()
    {
        ConstantNode node = new(null);
        string actual = node.ToODataString();
        actual.Should().Be("null");
    }

    [Fact]
    public void ToODataString_ConstantNode_Byte()
    {
        byte b = 0x42;
        ConstantNode node = new(b);
        string actual = node.ToODataString();
        actual.Should().Be("42");
    }

    [Theory]
    [InlineData('a', "'a'")]
    [InlineData('\'', "''''")]
    public void ToODataString_ConstantNode_Character(char c, string expected)
    {
        ConstantNode node = new(c);
        string actual = node.ToODataString();
        actual.Should().Be(expected);
    }

    [Fact]
    public void ToODataString_ConstantNode_Decimal()
    {
        decimal d = 42.0M;
        ConstantNode node = new(d);
        string actual = node.ToODataString();
        actual.Should().Be("42.0M");
    }

    [Theory]
    [InlineData(42.0, "42.0")]
    public void ToODataString_ConstantNode_Double(double d, string expected)
    {
        ConstantNode node = new(d);
        string actual = node.ToODataString();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(42.6f, "42.6f")]
    public void ToODataString_ConstantNode_Float(float d, string expected)
    {
        ConstantNode node = new(d);
        string actual = node.ToODataString();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(2001L, "2001L")]
    public void ToODataString_ConstantNode_Long(long l, string expected)
    {
        ConstantNode node = new(l);
        string actual = node.ToODataString();
        actual.Should().Be(expected);
    }

    [Fact]
    public void ToODataString_ConstantNode_Unknown()
    {
        Point p = new();
        ConstantNode node = new(p);
        string actual = node.ToODataString();
        actual.Should().Be("'{X=0,Y=0}'");
    }

    [Fact]
    public void ToODataString_BinaryOperatorKind_Unknown()
    {
        BinaryOperatorKind kind = (BinaryOperatorKind)100000;
        Action act = () => _ = kind.ToODataString();
        act.Should().Throw<NotSupportedException>();
    }
}
