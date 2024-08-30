// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

// There are lots of places where we .Append() and then ignore the result
// because we are not chaining.  This suppresses the warning.
#pragma warning disable IDE0058 // Expression value is never used

using CommunityToolkit.Datasync.Client.Query.Linq;
using System.Globalization;
using System.Text;

namespace CommunityToolkit.Datasync.Client.Query.OData;

internal class ODataExpressionVisitor : QueryNodeVisitor<QueryNode>
{
    /// <summary>
    /// The list of supported types for constants.
    /// </summary>
    internal enum ConstantType
    {
        Unknown,
        Null,
        Boolean,
        Byte,
        Character,
        Decimal,
        Double,
        Float,
        Int,
        Long,
        Short,
        SignedByte,
        UnsignedInt,
        UnsignedLong,
        UnsignedShort,
        StringArray,
        Date,
        TimeOfDay,
        DateTime,
        DateTimeOffset,
        Guid
    }

    /// <summary>
    /// Translates an expression tree of <see cref="QueryNode"/> elements
    /// to an OData expression.
    /// </summary>
    /// <param name="filter">The top <see cref="QueryNode"/> representing the entire expression.</param>
    /// <returns>An OData string.</returns>
    public static string ToODataQueryString(QueryNode filter)
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
    internal ODataExpressionVisitor()
    {
    }

    /// <summary>
    /// The OData expression.
    /// </summary>
    public StringBuilder Expression { get; } = new();

    /// <summary>
    /// Visit a <see cref="BinaryOperatorNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    public override QueryNode Visit(BinaryOperatorNode node)
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
    public override QueryNode Visit(ConstantNode node)
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
    public override QueryNode Visit(ConvertNode node)
    {
        throw new NotSupportedException("ConvertNode is not supported on the ODataExpressionVisitor");
    }

    /// <summary>
    /// Visit a <see cref="FunctionCallNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    public override QueryNode Visit(FunctionCallNode node)
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
    public override QueryNode Visit(MemberAccessNode node)
    {
        Expression.Append(node.MemberName);
        return node;
    }

    /// <summary>
    /// Visit a <see cref="UnaryOperatorNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    public override QueryNode Visit(UnaryOperatorNode node)
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

    /// <summary>
    /// Accept a visitor to a node, with error checking
    /// </summary>
    /// <param name="parent">The parent node</param>
    /// <param name="node">The node to visit</param>
    protected void Accept(QueryNode parent, QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(this);
    }
}
