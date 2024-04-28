// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using System.Text;

// We are walking expression trees, so the code uses a lot of method calls where the result is only rarely used.
#pragma warning disable IDE0058 // Expression value is never used

namespace CommunityToolkit.Datasync.Client.Query.OData;

/// <summary>
/// Translates an expression tree into an OData expression.
/// </summary>
internal class ODataExpressionVisitor : QueryNodeVisitor<QueryNode>
{
    /// <summary>
    /// Translates an expression tree of <see cref="QueryNode"/> elements
    /// to an OData expression.
    /// </summary>
    /// <param name="filter">The top <see cref="QueryNode"/> representing the entire expression.</param>
    /// <returns>An OData string.</returns>
    internal static string ToODataString(QueryNode filter)
    {
        if (filter == null)
        {
            return string.Empty;
        }

        ODataExpressionVisitor visitor = new();
        filter.Accept(visitor);
        return visitor.Expression.ToString();
    }

    /// <summary>
    /// You cannot instantiate this - access the visitor through the static methods.
    /// </summary>
    protected ODataExpressionVisitor()
    {
    }

    /// <summary>
    /// The OData expression.
    /// </summary>
    internal StringBuilder Expression { get; } = new();

    #region QueryNodeVisitor<QueryNode>
    /// <summary>
    /// Visit a <see cref="BinaryOperatorNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(BinaryOperatorNode node)
    {
        Expression.Append('(');
        Accept(node, node.LeftOperand);
        Expression.Append(' ').Append(node.OperatorKind.ToODataString()).Append(' ');
        Accept(node, node.RightOperand);
        Expression.Append(')');
        return node;
    }

    /// <summary>
    /// Visit a <see cref="ConstantNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(ConstantNode node)
    {
        Expression.Append(node.ToODataString());
        return node;
    }

    /// <summary>
    /// Visit a <see cref="ConvertNode"/>
    /// </summary>
    /// <remarks>
    /// This should never happen, but it's added for compatibility with the interface
    /// </remarks>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(ConvertNode node)
    {
        throw new NotSupportedException("ConvertNode is not supported on the ODataExpressionVisitor");
    }

    /// <summary>
    /// Visit a <see cref="FunctionCallNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(FunctionCallNode node)
    {
        // Special case: string[].Contains(string) is represented as "string in ( string, string, ...)" in OData
        if (node.Name == "in")
        {
            Accept(node, node.Arguments[1]);
            Expression.Append(" in ");
            Accept(node, node.Arguments[0]);
            return node;
        }

        bool appendSeparator = false;
        Expression.Append(node.Name).Append('(');
        foreach (QueryNode arg in node.Arguments)
        {
            if (appendSeparator)
            {
                Expression.Append(',');
            }

            Accept(node, arg);
            appendSeparator = true;
        }

        Expression.Append(')');
        return node;
    }

    /// <summary>
    /// Visit a <see cref="MemberAccessNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(MemberAccessNode node)
    {
        Expression.Append(node.MemberName);
        return node;
    }

    /// <summary>
    /// Visit a <see cref="UnaryOperatorNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(UnaryOperatorNode node)
    {
        switch (node.OperatorKind)
        {
            case UnaryOperatorKind.Not:
                Expression.Append("not(");
                Accept(node, node.Operand);
                Expression.Append(')');
                break;
            default:
                throw new NotSupportedException($"'{node.OperatorKind}' is not supported in a table query");
        }

        return node;
    }
    #endregion

    /// <summary>
    /// Accept a visitor to a node, with error checking
    /// </summary>
    /// <param name="parent">The parent node</param>
    /// <param name="node">The node to visit</param>
    protected void Accept(QueryNode parent, QueryNode node)
    {
        if (node == null)
        {
            throw new ArgumentException($"Parent {parent.Kind} is not complete.", nameof(node));
        }
        else
        {
            node.Accept(this);
        }
    }
}
