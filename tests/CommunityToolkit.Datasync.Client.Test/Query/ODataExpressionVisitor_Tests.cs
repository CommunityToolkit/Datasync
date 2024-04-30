// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using CommunityToolkit.Datasync.Client.Query.OData;

namespace CommunityToolkit.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class ODataExpressionVisitor_Tests
{
    private readonly TestODataExpressionVisitor visitor = new();
    [Fact]
    public void ToODataString_Null_ReturnsEmpty()
    {
        ODataExpressionVisitor.ToODataString(null).Should().BeEmpty();
    }

    [Fact]
    public void Visitor_ConvertNode_Throws()
    {
        ConvertNode node = new(new ConstantNode(1), typeof(int));
        Assert.ThrowsAny<Exception>(() => this.visitor.Visit(node));
    }

    [Fact]
    public void Visitor_UnaryNode_Throws()
    {
        UnaryOperatorNode node = new(UnaryOperatorKind.Negate, new ConstantNode(1));
        Assert.ThrowsAny<Exception>(() => this.visitor.Visit(node));
    }

    [Fact]
    public void Accept_Throws_OnIncompleteTypes()
    {
        ConstantNode node = new(1);
        Assert.ThrowsAny<Exception>(() => this.visitor.TestAccept(node, null));
    }

    private class TestODataExpressionVisitor : ODataExpressionVisitor
    {
        public void TestAccept(QueryNode parent, QueryNode node)
            => base.Accept(parent, node);
    }
}
