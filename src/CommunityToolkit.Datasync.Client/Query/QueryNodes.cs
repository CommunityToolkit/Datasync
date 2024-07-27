// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// The base class for all query nodes
/// </summary>
internal abstract class QueryNode
{
    /// <summary>
    /// Part of the visitor pattern, accept a <see cref="QueryNodeVisitor{T}"/> that
    /// walks a tree of type <see cref="QueryNode"/>.
    /// </summary>
    /// <typeparam name="T">The type that the visitor will return after visiting this token.</typeparam>
    /// <param name="visitor">The visitor.</param>
    /// <returns>An entity of the type accepted.</returns>
    internal abstract T Accept<T>(QueryNodeVisitor<T> visitor);

    /// <summary>
    /// The type of node that is being represented.
    /// </summary>
    internal abstract QueryNodeKind Kind { get; }

    /// <summary>
    /// Sets the children for this <see cref="QueryNode"/>.  Note that not all query node types support
    /// children, so this is not always called.  If it is called unexpectedly, then a <see cref="NotSupportedException"/>
    /// is thrown.
    /// </summary>
    /// <param name="children">The list of children</param>
    /// <exception cref="NotSupportedException">If this method is called unexpectedly.</exception>
    internal virtual void SetChildren(IList<QueryNode> children)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// A <see cref="QueryNode"/> representing a binary operator
/// </summary>
/// <param name="kind">The type of binary operator.</param>
/// <param name="left">The left operand.</param>
/// <param name="right">The right operand.</param>
internal class BinaryOperatorNode(BinaryOperatorKind kind, QueryNode? left = null, QueryNode? right = null) : QueryNode
{
    /// <inheritdoc/>
    internal override QueryNodeKind Kind => QueryNodeKind.BinaryOperator;

    /// <inheritdoc />
    internal override T Accept<T>(QueryNodeVisitor<T> visitor)
        => visitor.Visit(this);

    /// <inheritdoc />
    internal override void SetChildren(IList<QueryNode> children)
    {
        ThrowIf.CountMismatch(children, 2, nameof(children));
        LeftOperand = children[0];
        RightOperand = children[1];
    }

    /// <summary>
    /// The type of binary operator.
    /// </summary>
    internal BinaryOperatorKind OperatorKind { get; } = kind;

    /// <summary>
    /// The left side of the operation
    /// </summary>
    internal QueryNode? LeftOperand { get; set; } = left;

    /// <summary>
    /// The right side of the operation
    /// </summary>
    internal QueryNode? RightOperand { get; set; } = right;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a primitive constant value.
/// </summary>
/// <param name="value">The value of the constant.</param>
internal class ConstantNode(object value) : QueryNode
{
    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.Constant;

    /// <inheritdoc />
    internal override T Accept<T>(QueryNodeVisitor<T> visitor)
        => visitor.Visit(this);

    /// <summary>
    /// The value of the primitive constant.
    /// </summary>
    internal object Value { get; set; } = value;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a conversion from one type to another.
/// </summary>
/// <param name="source">The source value to convert.</param>
/// <param name="targetType">The target type of the conversion.</param>
internal class ConvertNode(QueryNode? source, Type targetType) : QueryNode
{
    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.Convert;

    /// <inheritdoc/>
    internal override T Accept<T>(QueryNodeVisitor<T> visitor)
        => visitor.Visit(this);

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        ThrowIf.CountMismatch(children, 1, nameof(children));
        Source = children[0];
    }

    /// <summary>
    /// The source value to convert
    /// </summary>
    internal QueryNode? Source { get; set; } = source;

    /// <summary>
    /// The type that we're converting to
    /// </summary>
    internal Type TargetType { get; set; } = targetType;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a function call
/// </summary>
/// <param name="name">The name of the function to call.</param>
/// <param name="arguments">The list of arguments to this function call.</param>
internal class FunctionCallNode(string name, IList<QueryNode>? arguments = null) : QueryNode
{
    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.FunctionCall;

    /// <inheritdoc/>
    internal override T Accept<T>(QueryNodeVisitor<T> visitor)
        => visitor.Visit(this);

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        Arguments = [.. children];
    }

    /// <summary>
    /// The name of the function to call.
    /// </summary>
    internal string Name { get; set; } = name;

    /// <summary>
    /// The list of arguments to this function call.
    /// </summary>
    internal IList<QueryNode> Arguments { get; set; } = arguments ?? [];
}

/// <summary>
/// A <see cref="QueryNode"/> representing access to a member value
/// </summary>
/// <param name="instance">The object instance we are accessing.</param>
/// <param name="memberName">The name of the member we are accessing.</param>
internal class MemberAccessNode(QueryNode? instance, string memberName) : QueryNode
{
    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.MemberAccess;

    /// <inheritdoc/>
    internal override T Accept<T>(QueryNodeVisitor<T> visitor)
        => visitor.Visit(this);

    /// <summary>
    /// The object instance we are accessing
    /// </summary>
    internal QueryNode? Instance { get; set; } = instance;

    /// <summary>
    /// The name of the member (property, field, etc.) we are acccessing
    /// </summary>
    internal string MemberName { get; set; } = memberName;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a unary operator.
/// </summary>
/// <param name="kind">The unary operator represented by this node.</param>
/// <param name="operand">The operand of the unary operator.</param>
internal class UnaryOperatorNode(UnaryOperatorKind kind, QueryNode? operand) : QueryNode
{
    /// <inheritdoc/>
    internal override QueryNodeKind Kind => QueryNodeKind.UnaryOperator;

    /// <inheritdoc/>
    internal override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        ThrowIf.CountMismatch(children, 1, nameof(children));
        Operand = children[0];
    }

    /// <summary>
    /// The operand of the unary operator.
    /// </summary>
    internal QueryNode? Operand { get; set; } = operand;

    /// <summary>
    /// The operator represented by this node.
    /// </summary>
    internal UnaryOperatorKind OperatorKind { get; set; } = kind;
}
