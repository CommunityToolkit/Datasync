// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0017 // Simplify object initialization

using CommunityToolkit.Datasync.Client.Query.Linq;
using NSubstitute;

namespace CommunityToolkit.Datasync.Client.Test.Query.Linq;

[ExcludeFromCodeCoverage]
public class QueryNode_Tests
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
        Action act = () => node.SetChildren([new ConstantNode(1)]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UnaryOperatorNode_NeedsOneArgument()
    {
        UnaryOperatorNode node = new(UnaryOperatorKind.Not, new ConstantNode(1));
        Action act = () => node.SetChildren([new ConstantNode(1), new ConstantNode(2)]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MemberAccessNode_GetInstance()
    {
        BinaryOperatorNode instance = new(BinaryOperatorKind.Add);
        MemberAccessNode node = new(instance, "field");
        node.Instance.Should().BeSameAs(instance);
    }

    [Fact]
    public void ConstantNode_SetChildren_Throws()
    {
        ConstantNode node = new(true);
        Assert.ThrowsAny<Exception>(() => node.SetChildren([node]));
    }

    [Fact]
    public void ConstantNode_SetValue_Works()
    {
        ConstantNode node = new(null);
        node.Value = "string";
        node.Value.Should().Be("string");
    }

    [Fact]
    public void ConvertNode_SetChildren_Works()
    {
        ConvertNode instance = new(null, typeof(string));
        List<QueryNode> children = [new ConstantNode("abc123")];
        instance.SetChildren(children);
        Assert.Same(children[0], instance.Source);
        Assert.Equal(typeof(string), instance.TargetType);
    }

    [Fact]
    public void ConvertNode_SetChildren_ThrowsException()
    {
        ConvertNode instance = new(null, typeof(string));
        Assert.ThrowsAny<Exception>(() => instance.SetChildren(Array.Empty<QueryNode>()));
    }

    [Fact]
    public void ConvertNode_SetType_Works()
    {
        ConvertNode instance = new(null, null) { TargetType = typeof(string) };
        instance.TargetType.Should().Be(typeof(string));
    }

    [Fact]
    public void FunctionCallNode_SetName_Works()
    {
        FunctionCallNode node = new("foo");
        node.Name = "bar";
        node.Name.Should().Be("bar");
        node.SetChildren(null);
        node.Arguments.Should().BeEmpty();
        node.SetChildren([]);
        node.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void MemberAccessNode_Sets_Work()
    {
        ConstantNode instance = new("42");
        MemberAccessNode node = new(null, string.Empty);
        node.Instance = instance;
        node.MemberName = "name";
        node.Instance.Should().BeSameAs(instance);
        node.MemberName.Should().Be("name");
    }

    [Fact]
    public void UnaryOperator_Node_SetChildren_Works()
    {
        ConstantNode child = new("42");
        UnaryOperatorNode node = new(UnaryOperatorKind.Negate, null);
        node.SetChildren([child]);
        node.OperatorKind = UnaryOperatorKind.Not;
        node.Operand.Should().BeSameAs(child);
        node.OperatorKind.Should().Be(UnaryOperatorKind.Not);

        ConstantNode newChild = new("50");
        node.Operand = newChild;
        node.Operand.Should().BeSameAs(newChild);
    }

    [Fact]
    public void Accept_CallsVisitor()
    {
        VisitorProxy visitor = new();
        new BinaryOperatorNode(BinaryOperatorKind.Add).Accept(visitor).Should().Be("binaryOperator");
        new ConstantNode("foo").Accept(visitor).Should().Be("constant");
        new ConvertNode(null, typeof(string)).Accept(visitor).Should().Be("convert");
        new FunctionCallNode("foo").Accept(visitor).Should().Be("functionCall");
        new MemberAccessNode(null, "member").Accept(visitor).Should().Be("memberAccess");
        new UnaryOperatorNode(UnaryOperatorKind.Negate, null).Accept(visitor).Should().Be("unaryOperator");
    }

    internal class VisitorProxy : QueryNodeVisitor<string>
    {
        public override string Visit(BinaryOperatorNode node) => "binaryOperator";
        public override string Visit(ConstantNode node) => "constant";
        public override string Visit(ConvertNode node) => "convert";
        public override string Visit(FunctionCallNode node) => "functionCall";
        public override string Visit(MemberAccessNode node) => "memberAccess";
        public override string Visit(UnaryOperatorNode node) => "unaryOperator";
    }
}
