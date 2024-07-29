// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.OData;

namespace CommunityToolkit.Datasync.Client.Test.Query.OData;

/// <summary>
/// Corner cases only
/// </summary>
[ExcludeFromCodeCoverage]
public class ODataExpressionVisitor_Tests
{
    [Fact]
    public void ToODataQueryString_Null()
    {
        QueryNode node = null;
        string actual = ODataExpressionVisitor.ToODataQueryString(node);
        actual.Should().Be(string.Empty);
    }

    [Fact]
    public void Visit_ConvertNode()
    {
        ConstantNode constantNode = new(42);
        ConvertNode node = new(constantNode, typeof(decimal));
        ODataExpressionVisitor visitor = new();
        Action act = () => visitor.Visit(node);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Visit_UnaryOperatorNode()
    {
        ConstantNode constantNode = new(42);
        UnaryOperatorNode node = new(UnaryOperatorKind.Negate, constantNode);
        ODataExpressionVisitor visitor = new();
        Action act = () => visitor.Visit(node);
        act.Should().Throw<NotSupportedException>();
    }
}
