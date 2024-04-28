// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// Enumeration of the supported unary operators.
/// </summary>
internal enum UnaryOperatorKind
{
    Negate,
    Not
}

/// <summary>
/// A <see cref="QueryNode"/> representing a unary operator.
/// </summary>
/// <param name="kind">The kind of unary operator</param>
/// <param name="operand">The operand of the unary operator</param>
internal class UnaryOperatorNode(UnaryOperatorKind kind, QueryNode operand) : QueryNode
{
    /// <inheritdoc/>
    internal override QueryNodeKind Kind => QueryNodeKind.UnaryOperator;

    /// <summary>
    /// The operand of the unary operator.
    /// </summary>
    internal QueryNode Operand { get; set; } = operand;

    /// <summary>
    /// The operator represented by this node.
    /// </summary>
    internal UnaryOperatorKind OperatorKind { get; set; } = kind;

    /// <inheritdoc/>
    internal override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        Ensure.That(children, nameof(children)).IsNotNull();
        Ensure.That(children.Count, nameof(children)).Is(1);
        Operand = children[0];
    }
}
