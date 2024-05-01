// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// Enumerator of the supported binary operators.
/// </summary>
internal enum BinaryOperatorKind
{
    Or = 0,
    And,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo
}

/// <summary>
/// A set of extension methods dealing with the <see cref="BinaryOperatorNode"/> and related objects.
/// </summary>
internal static class BinaryOperatorExtensions
{
    /// <summary>
    /// Convert an <see cref="ExpressionType"/> into a <see cref="BinaryOperatorKind"/>
    /// </summary>
    /// <param name="type">The <see cref="ExpressionType"/></param>
    /// <returns>The equivalent <see cref="BinaryOperatorKind"/></returns>
    internal static BinaryOperatorKind ToBinaryOperatorKind(this ExpressionType type) => type switch
    {
        ExpressionType.Add => BinaryOperatorKind.Add,
        ExpressionType.AndAlso => BinaryOperatorKind.And,
        ExpressionType.Divide => BinaryOperatorKind.Divide,
        ExpressionType.Equal => BinaryOperatorKind.Equal,
        ExpressionType.GreaterThan => BinaryOperatorKind.GreaterThan,
        ExpressionType.GreaterThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual,
        ExpressionType.LessThan => BinaryOperatorKind.LessThan,
        ExpressionType.LessThanOrEqual => BinaryOperatorKind.LessThanOrEqual,
        ExpressionType.Modulo => BinaryOperatorKind.Modulo,
        ExpressionType.Multiply => BinaryOperatorKind.Multiply,
        ExpressionType.NotEqual => BinaryOperatorKind.NotEqual,
        ExpressionType.OrElse => BinaryOperatorKind.Or,
        ExpressionType.Subtract => BinaryOperatorKind.Subtract,
        _ => throw new NotSupportedException($"The operator '{type}' is not supported in the 'Where' query expression")
    };
}

/// <summary>
/// A <see cref="QueryNode"/> representing a binary operator
/// </summary>
/// <param name="kind">The type of binary operator</param>
/// <param name="left">The left side of the operation</param>
/// <param name="right">The right side of the operation</param>
internal class BinaryOperatorNode(BinaryOperatorKind kind, QueryNode left = null, QueryNode right = null) : QueryNode
{
    /// <inheritdoc/>
    internal override QueryNodeKind Kind => QueryNodeKind.BinaryOperator;

    /// <summary>
    /// The type of binary operator.
    /// </summary>
    internal BinaryOperatorKind OperatorKind { get; } = kind;

    /// <summary>
    /// The left side of the operation
    /// </summary>
    internal QueryNode LeftOperand { get; set; } = left;

    /// <summary>
    /// The right side of the operation
    /// </summary>
    internal QueryNode RightOperand { get; set; } = right;

    /// <inheritdoc />
    internal override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc />
    internal override void SetChildren(IList<QueryNode> children)
    {
        Ensure.That(children, nameof(children)).IsNotNull().And.HasCount(2);
        LeftOperand = children[0];
        RightOperand = children[1];
    }
}