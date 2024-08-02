// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// Enumeration for the different kinds of QueryNode.
/// </summary>
internal enum QueryNodeKind
{
    BinaryOperator,
    Constant,
    Convert,
    FunctionCall,
    MemberAccess,
    UnaryOperator,
}

/// <summary>
/// The base class for all types of QueryNode.
/// </summary>
internal abstract class QueryNode
{
    /// <summary>
    /// Accept a QueryNodeVisitor that walks a tree of type QueryNode.
    /// </summary>
    /// <typeparam name="T">The type that the visitor will return after visiting this token.</typeparam>
    /// <param name="visitor">The visitor.</param>
    public abstract T Accept<T>(QueryNodeVisitor<T> visitor);

    /// <summary>
    /// The type of the QueryNode
    /// </summary>
    public abstract QueryNodeKind Kind { get; }

    /// <summary>
    /// Sets the children for this QueryNode.  Note that not all query nodes support
    /// children, so this is not always called.  If it is called when unexpected, a
    /// <see cref="NotSupportedException"/> will be thrown.
    /// </summary>
    /// <param name="children">The list of children to set.</param>
    internal virtual void SetChildren(IList<QueryNode> children)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// A <see cref="QueryNode"/> representing a binary operator
/// </summary>
/// <param name="kind">The type of the binary operator.</param>
/// <param name="left">The left operand.</param>
/// <param name="right">The right operand.</param>
internal sealed class BinaryOperatorNode(BinaryOperatorKind kind, QueryNode left = null, QueryNode right = null) : QueryNode
{
    /// <inheritdoc />
    public override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    public override QueryNodeKind Kind => QueryNodeKind.BinaryOperator;

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
    public BinaryOperatorKind OperatorKind { get; } = kind;

    /// <summary>
    /// The left side of the operation
    /// </summary>
    public QueryNode LeftOperand { get; set; } = left;

    /// <summary>
    /// The right side of the operation
    /// </summary>
    public QueryNode RightOperand { get; set; } = right;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a primitive constant value.
/// </summary>
/// <param name="value">The value of the constant.</param>
internal sealed class ConstantNode(object value) : QueryNode
{
    /// <inheritdoc />
    public override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override QueryNodeKind Kind => QueryNodeKind.Constant;

    /// <summary>
    /// The value of the primitive constant.
    /// </summary>
    public object Value { get; set; } = value;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a conversion from one type to another.
/// </summary>
/// <param name="source">The expression to be converted.</param>
/// <param name="targetType">The target type for the conversion.</param>
internal sealed class ConvertNode(QueryNode source, Type targetType) : QueryNode
{
    /// <inheritdoc/>
    public override T Accept<T>(QueryNodeVisitor<T> visitor)
        => visitor.Visit(this);

    /// <inheritdoc />
    public override QueryNodeKind Kind => QueryNodeKind.Convert;

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        ThrowIf.NullOrEmpty(children, nameof(children));
        Source = children[0];
    }

    /// <summary>
    /// The source value to convert
    /// </summary>
    public QueryNode Source { get; set; } = source;

    /// <summary>
    /// The type that we're converting to
    /// </summary>
    public Type TargetType { get; set; } = targetType;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a function call
/// </summary>
internal sealed class FunctionCallNode(string name, IList<QueryNode> arguments = null) : QueryNode
{
    /// <inheritdoc/>
    public override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override QueryNodeKind Kind => QueryNodeKind.FunctionCall;

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        Arguments = children?.ToList() ?? [];
    }

    /// <summary>
    /// The name of the function to call.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The list of arguments to this function call.
    /// </summary>
    public IList<QueryNode> Arguments { get; set; } = arguments ?? [];
}

/// <summary>
/// A <see cref="QueryNode"/> representing access to a member value
/// </summary>
internal sealed class MemberAccessNode(QueryNode instance, string memberName) : QueryNode
{
    /// <inheritdoc/>
    public override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <summary>
    /// The object instance we are accessing
    /// </summary>
    public QueryNode Instance { get; set; } = instance;

    /// <inheritdoc />
    public override QueryNodeKind Kind => QueryNodeKind.MemberAccess;

    /// <summary>
    /// The name of the member (property, field, etc.) we are acccessing
    /// </summary>
    public string MemberName { get; set; } = memberName;
}

/// <summary>
/// A <see cref="QueryNode"/> representing a unary operator.
/// </summary>
/// <param name="kind">The operator kind for the operation.</param>
/// <param name="operand">The operation operand.</param>
internal sealed class UnaryOperatorNode(UnaryOperatorKind kind, QueryNode operand) : QueryNode
{
    /// <inheritdoc/>
    public override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    public override QueryNodeKind Kind => QueryNodeKind.UnaryOperator;

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        ThrowIf.CountMismatch(children, 1, nameof(children));
        Operand = children[0];
    }

    /// <summary>
    /// The operand of the unary operator.
    /// </summary>
    public QueryNode Operand { get; set; } = operand;

    /// <summary>
    /// The operator represented by this node.
    /// </summary>
    public UnaryOperatorKind OperatorKind { get; set; } = kind;
}
