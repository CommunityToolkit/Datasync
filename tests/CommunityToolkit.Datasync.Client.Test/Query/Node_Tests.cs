// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

namespace CommunityToolkit.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class Node_Tests
{
    [Fact]
    public void AllNodes_Kind_SetCorrectly()
    {
        QueryNode node = new ConstantNode(true);

        Assert.Equal(QueryNodeKind.BinaryOperator, new BinaryOperatorNode(BinaryOperatorKind.Or).Kind);
        Assert.Equal(QueryNodeKind.Constant, new ConstantNode(true).Kind);
        Assert.Equal(QueryNodeKind.Convert, new ConvertNode(node, typeof(QueryNode)).Kind);
        Assert.Equal(QueryNodeKind.FunctionCall, new FunctionCallNode("name").Kind);
        Assert.Equal(QueryNodeKind.MemberAccess, new MemberAccessNode(null, "updatedAt").Kind);
        Assert.Equal(QueryNodeKind.UnaryOperator, new UnaryOperatorNode(UnaryOperatorKind.Not, node).Kind);
    }

    [Fact]
    public void BinaryOperatorNode_NeedsTwoArguments()
    {
        BinaryOperatorNode node = new(BinaryOperatorKind.Add);
        QueryNode[] children = [new ConstantNode(1), new ConstantNode(2)];
        node.SetChildren(children);

        children = [new ConstantNode(1)];
        Assert.ThrowsAny<Exception>(() => node.SetChildren(children));
    }

    [Fact]
    public void UnaryOperatorNode_NeedsOneArgument()
    {
        UnaryOperatorNode node = new(UnaryOperatorKind.Not, new ConstantNode(1));
        QueryNode[] children = [new ConstantNode(1), new ConstantNode(2)];
        Assert.ThrowsAny<Exception>(() => node.SetChildren(children));

        children = [new ConstantNode(1)];
        node.SetChildren(children); // does not throw
    }

    [Fact]
    public void UnaryOperatorNode_RetroSet()
    {
        UnaryOperatorNode node = new(UnaryOperatorKind.Not, null) { OperatorKind = UnaryOperatorKind.Negate };
        IList<QueryNode> children = [new ConstantNode(1)];
        node.SetChildren(children);
        node.OperatorKind.Should().Be(UnaryOperatorKind.Negate);
        node.Operand.Should().BeSameAs(children[0]);
    }

    [Fact]
    public void FunctionCallNode_SetName()
    {
        FunctionCallNode node = new(null) { Name = "name" };
        QueryNode[] children = [new ConstantNode(1), new ConstantNode(2)];
        node.SetChildren(children);
        node.Name.Should().Be("name");
        node.Arguments.Should().BeEquivalentTo(children);
    }

    [Fact]
    public void MemberAccessNode_GetInstance()
    {
        BinaryOperatorNode instance = new(BinaryOperatorKind.Add);
        MemberAccessNode node = new(instance, "field");
        node.Instance.Should().BeSameAs(instance);

        ConstantNode parent = new(20);
        MemberAccessNode after = new(null, null) { Instance = parent, MemberName = "field" };
        after.Instance.Should().BeSameAs(parent);
        after.MemberName.Should().Be("field");
    }

    [Fact]
    public void ConstantNode_SetChildren_Throws()
    {
        ConstantNode node = new(true);
        Assert.ThrowsAny<Exception>(() => node.SetChildren([node]));

        ConstantNode after = new(null) { Value = true };
        after.Value.Should().Be(true);
    }

    [Fact]
    public void ConvertNode_SetChildren_Works()
    {
        ConvertNode instance = new(null, typeof(string));
        List<QueryNode> children = [new ConstantNode("abc123")];
        instance.SetChildren(children);
        instance.Source.Should().BeSameAs(children[0]);
        instance.TargetType.Should().Be(typeof(string));

        ConvertNode after = new(null, null) { TargetType = typeof(string) };
        after.SetChildren(children);
        instance.Source.Should().BeSameAs(children[0]);
        instance.TargetType.Should().Be(typeof(string));
    }

    [Fact]
    public void ConvertNode_SetChildren_ThrowsException()
    {
        ConvertNode instance = new(null, typeof(string));
        Assert.ThrowsAny<Exception>(() => instance.SetChildren([]));
    }
}
