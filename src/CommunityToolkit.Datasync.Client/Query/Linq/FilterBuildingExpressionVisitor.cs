// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

// Stylistically, this file has a couple of switch statements that are long,
// and we've opted for the more readable version of these switch statements.
#pragma warning disable IDE0066 // Convert switch statement to expression

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// Walks the LINQ expression tree to compile the expression into a simplified tree of
/// nodes that can be used for OData query string production.
/// </summary>
internal sealed class FilterBuildingExpressionVisitor
{
    #region DO NOT DELETE THIS SECTION
    /// <summary>
    /// <para>** Do not remove this code. **</para>
    /// </summary>
    /// <remarks>
    /// Some compilers will remove method infos that are never called by an application.
    /// This will break reflection scenarios when the methodInfos searched for via reflection
    /// were not used in the application code and so were removed by the compiler. We search
    /// for the methodInfos for Object.ToString() and String.Concat(string, string) via
    /// reflection, so we need this code here to ensure that don't get removed by the compiler.
    /// </remarks>
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "See code comments - this is required!!!")]
    static FilterBuildingExpressionVisitor()
    {
        string aString = new object().ToString();
        aString = string.Concat(aString, "a string");
    }
    #endregion

    // MethodInfo for string.Concat(string, string)
    private static readonly MethodInfo concatMethod = typeof(string).GetRuntimeMethod("Concat", [typeof(string), typeof(string)]);
    // MethodInfo for string.StartsWith(string, StringComparison)
    private static readonly MethodInfo startsWithMethod = typeof(string).GetRuntimeMethod("StartsWith", [typeof(string), typeof(StringComparison)]);
    // MethodInfo for string.EndsWith(string, StringComparison)
    private static readonly MethodInfo endsWithMethod = typeof(string).GetRuntimeMethod("EndsWith", [typeof(string), typeof(StringComparison)]);
    // MethodInfo for string.Equals(string)
    private static readonly MethodInfo equals1Method = typeof(string).GetRuntimeMethod("Equals", [typeof(string)]);
    // MethodInfo for string.Equals(string, StringComparison)
    private static readonly MethodInfo equals2Method = typeof(string).GetRuntimeMethod("Equals", [typeof(string), typeof(StringComparison)]);
    // MethodInfo for IEnumerable<string>.Contains(string) - generic extension method
    private static readonly MethodInfo arrayContainsMethod = typeof(Enumerable).GetRuntimeMethods().Single(m => m.Name == "Contains" && m.GetParameters().Length == 2).MakeGenericMethod(typeof(string));
    // MethodInfo for string.ToString()
    private static readonly MethodInfo toStringMethod = typeof(object).GetTypeInfo().GetDeclaredMethod("ToString");

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterBuildingExpressionVisitor"/>
    /// </summary>
    /// <param name="namingPolicy">The <see cref="JsonNamingPolicy"/> to use to determine property names from members used with expressions.</param>
    internal FilterBuildingExpressionVisitor(JsonNamingPolicy namingPolicy) : base()
    {
        ArgumentNullException.ThrowIfNull(namingPolicy, nameof(namingPolicy));
        NamingPolicy = namingPolicy;
    }

    /// <summary>
    /// The naming policy to use in converting member names into a serialized property name.
    /// </summary>
    internal JsonNamingPolicy NamingPolicy { get; }

    /// <summary>
    /// The accumulator for the <see cref="QueryNode"/> representing the filter in the expression tree.
    /// </summary>
    internal Stack<QueryNode> FilterExpression { get; } = new();

    /// <summary>
    /// Compiles an expression tree representing the predicate for a filter into a query node stack.
    /// </summary>
    /// <param name="filterExpression">The filter expression to compile.</param>
    /// <param name="namingPolicy">The naming policy for converting member names into serialized property names.</param>
    /// <returns>The root <see cref="QueryNode"/> for the compiled filter expression.</returns>
    internal static QueryNode Compile(Expression filterExpression, JsonNamingPolicy namingPolicy)
    {
        FilterBuildingExpressionVisitor visitor = new(namingPolicy);
        _ = visitor.Visit(filterExpression);
        return visitor.FilterExpression.FirstOrDefault();
    }

    /// <summary>
    /// Gets the table member name referenced by an expression, or return null.
    /// </summary>
    /// <param name="expression">The expression to check</param>
    /// <param name="namingPolicy">The naming policy for converting member names into serialized property names.</param>
    /// <returns>the table member name</returns>
    internal static string GetTableMemberName(Expression expression, JsonNamingPolicy namingPolicy)
    {
        ArgumentNullException.ThrowIfNull(expression, nameof(expression));
        ArgumentNullException.ThrowIfNull(namingPolicy, nameof(namingPolicy));

        if (expression is MemberExpression member)
        {
            if (member.Expression.NodeType == ExpressionType.Parameter)
            {
                return namingPolicy?.ConvertName(member.Member.Name) ?? member.Member.Name;
            }

            if (member.Expression.NodeType == ExpressionType.Convert && member.Member.MemberType == MemberTypes.Property)
            {
                return namingPolicy?.ConvertName(member.Member.Name) ?? member.Member.Name;
            }
        }

        return null;
    }

    /// <summary>
    /// Visits a node of the expression and all sub-nodes, effectively walking the expression tree.
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node.</returns>
    internal Expression Visit(Expression node)
    {
        if (node == null)
        {
            return null;
        }

        switch (node.NodeType)
        {
            case ExpressionType.Add:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Coalesce:
            case ExpressionType.Divide:
            case ExpressionType.Equal:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.NotEqual:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.Power:
            case ExpressionType.RightShift:
            case ExpressionType.Subtract:
                return VisitBinaryExpression((BinaryExpression)node);

            case ExpressionType.Constant:
                return VisitConstantExpression((ConstantExpression)node);

            case ExpressionType.MemberAccess:
                return VisitMemberExpression((MemberExpression)node);

            case ExpressionType.Call:
                return VisitMethodCallExpression((MethodCallExpression)node);

            case ExpressionType.Convert: // TODO: Validate that this is the correct mechanism and not a ConvertExpression instead!
            case ExpressionType.Not:
            case ExpressionType.Quote:
                return VisitUnaryExpression((UnaryExpression)node);

            default:
                throw new NotSupportedException($"'{node}' is not supported in a 'Where' query expression");
        }
    }

    /// <summary>
    /// Process a binary expression
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns>The visited expression</returns>
    internal Expression VisitBinaryExpression(BinaryExpression node)
    {
        if (IsEnumExpression(node, out UnaryExpression enumExpression, out ConstantExpression constant) && enumExpression != null && constant != null)
        {
            Type enumType = enumExpression.Operand.Type;
            string enumString = Enum.ToObject(enumType, constant.Value).ToString();
            Expression call = Expression.Call(enumExpression, toStringMethod);
            _ = Visit(Expression.MakeBinary(node.NodeType, call, Expression.Constant(enumString)));
        }
        else if (node.NodeType == ExpressionType.Add && node.Left.Type == typeof(string) && node.Right.Type == typeof(string))
        {
            _ = Visit(Expression.Call(concatMethod, [node.Left, node.Right]));
        }
        else
        {
            BinaryOperatorNode op = new(node.NodeType.ToBinaryOperatorKind());
            FilterExpression.Push(op);
            _ = Visit(node.Left);
            _ = Visit(node.Right);
            SetChildren(op);
        }

        return node;
    }

    /// <summary>
    /// Process a constant expression
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns>The visited expression</returns>
    private ConstantExpression VisitConstantExpression(ConstantExpression node)
    {
        FilterExpression.Push(new ConstantNode(node.Value));
        return node;
    }

    /// <summary>
    /// Process a member expression
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns>The visited expression</returns>
    private MemberExpression VisitMemberExpression(MemberExpression node)
    {
        // Is the member the name of a member?
        string memberName = GetTableMemberName(node, NamingPolicy);
        if (memberName != null)
        {
            FilterExpression.Push(new MemberAccessNode(null, memberName));
            return node;
        }

        // Is this member actually a function that looks like a property (e.g. string.Length)
        string methodName = InstanceProperties.GetMethodName(new MemberInfoKey(node.Member));
        if (methodName != null)
        {
            FunctionCallNode fnCallNode = new(methodName);
            FilterExpression.Push(fnCallNode);
            _ = Visit(node.Expression);
            SetChildren(fnCallNode);
            return node;
        }

        // Otherwise we don't support it
        throw new NotSupportedException($"The member '{node.Member.Name} is not supported in the 'Where' clause");
    }

    /// <summary>
    /// Process method calls for OData
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal Expression VisitMethodCallExpression(MethodCallExpression node)
    {
        MethodInfo baseDefinition = node.Method.GetRuntimeBaseDefinition();

        //
        // string.StartsWith(string, StringComparison)
        //
        if (baseDefinition.Equals(startsWithMethod) && node.Arguments.Count == 2 && node.Arguments[1].Type == typeof(StringComparison))
        {
            FunctionCallNode callNode = new("startswith");
            FilterExpression.Push(callNode);
            _ = VisitStringComparisonExpression(callNode, node);
            return node;
        }

        //
        // string.EndsWith(string, StringComparison)
        //
        if (baseDefinition.Equals(endsWithMethod) && node.Arguments.Count == 2 && node.Arguments[1].Type == typeof(StringComparison))
        {
            FunctionCallNode callNode = new("endswith");
            FilterExpression.Push(callNode);
            _ = VisitStringComparisonExpression(callNode, node);
            return node;
        }

        //
        // string.Equals(string)
        //
        if (baseDefinition.Equals(equals1Method) && node.Arguments.Count == 1)
        {
            BinaryOperatorNode equalityNode = new(BinaryOperatorKind.Equal);
            FilterExpression.Push(equalityNode);
            _ = Visit(node.Object);
            _ = Visit(node.Arguments[0]);
            SetChildren(equalityNode);
            return node;
        }

        //
        // string.Equals(string, StringComparison)
        //
        if (baseDefinition.Equals(equals2Method) && node.Arguments.Count == 2 && node.Arguments[1].Type == typeof(StringComparison))
        {
            BinaryOperatorNode equalityNode = new(BinaryOperatorKind.Equal);
            FilterExpression.Push(equalityNode);
            _ = VisitStringComparisonExpression(equalityNode, node);
            return node;
        }

        //
        // string[].Contains(string)
        //
        if (baseDefinition.Equals(arrayContainsMethod) && node.Arguments.Count == 2 && node.Arguments[0].Type == typeof(string[]))
        {
            FunctionCallNode callNode = new("in");
            FilterExpression.Push(callNode);
            _ = Visit(node.Arguments[0]);
            _ = Visit(node.Arguments[1]);
            SetChildren(callNode);
            return node;
        }

        //
        // Anything in the MethodNames table (which has a direct relationship to the OData representation)
        //
        if (MethodNames.TryGetValue(new MemberInfoKey(node.Method), out string methodName, out bool isStatic))
        {
            FunctionCallNode fnCallNode = new(methodName);
            FilterExpression.Push(fnCallNode);
            foreach (Expression argument in (isStatic ? [] : new Expression[] { node.Object }).Concat(node.Arguments))
            {
                _ = Visit(argument);
            }

            SetChildren(fnCallNode);
            return node;
        }

        //
        // string.ToString()
        //
        if (baseDefinition.Equals(toStringMethod))
        {
            _ = Visit(node.Object);
            return node;
        }

        //
        // Default case
        //
        throw new NotSupportedException($"'{node}' is not supported in a 'Where' clause");
    }

    /// <summary>
    /// Process a two-argument string-expression (startsWith, endsWith)
    /// </summary>
    /// <param name="queryNode">The <see cref="QueryNode"/> being created.</param>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal Expression VisitStringComparisonExpression(QueryNode queryNode, MethodCallExpression node)
    {
        StringComparison comparison = GetStringComparisonFromExpression(node.Arguments[1]);
        switch (comparison)
        {
            case StringComparison.Ordinal:
            case StringComparison.InvariantCulture:
                _ = Visit(node.Object);
                _ = Visit(node.Arguments[0]);
                SetChildren(queryNode);
                return node;

            case StringComparison.OrdinalIgnoreCase:
            case StringComparison.InvariantCultureIgnoreCase:
                FunctionCallNode arg1 = new("tolower");
                FilterExpression.Push(arg1);
                _ = Visit(node.Object);
                SetChildren(arg1);

                FunctionCallNode arg2 = new("tolower");
                FilterExpression.Push(arg2);
                _ = Visit(node.Arguments[0]);
                SetChildren(arg2);

                SetChildren(queryNode);
                return node;

            default:
                throw new NotSupportedException($"'{node}' is not using a supported StringComparison value");
        }
    }

    /// <summary>
    /// When there is a StringComparison node, find it's value.  The backend doesn't
    /// support StringComparison, so remove it from the stack.
    /// </summary>
    /// <param name="node">The expression.</param>
    /// <returns>The StringComparison value.</returns>
    internal StringComparison GetStringComparisonFromExpression(Expression node)
    {
        // Get the StringComparison argument by visiting the node, but don't leave it on the stack
        _ = Visit(node);
        QueryNode comparisonNode = FilterExpression.Pop();

        // We expect it to be a constant node of type StringComparison
        if (comparisonNode is not ConstantNode ccn || ccn.Value.GetType() != typeof(StringComparison))
        {
            throw new NotSupportedException($"'{node}' must have a constant StringComparison value");
        }

        // If it is, then return the value.
        return (StringComparison)ccn.Value;
    }

    /// <summary>
    /// Process unary expressions
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal Expression VisitUnaryExpression(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                _ = Visit(node.Operand);
                FilterExpression.Push(new UnaryOperatorNode(UnaryOperatorKind.Not, FilterExpression.Pop()));
                break;
            case ExpressionType.Quote:
                _ = Visit(node.Operand);
                break;
            case ExpressionType.Convert:
                if (!IsConversionImplicit(node, node.Operand.Type, node.Type))
                {
                    throw new NotSupportedException($"Implicit conversion from '{node.Operand.Type}' to '{node.Type}' is not supported by a 'Where' {node.NodeType}' clause.");
                }

                _ = Visit(node.Operand);
                break;
            default:
                throw new NotSupportedException($"The operator '{node.NodeType}' is not supported in a 'Where' clause");
        }

        return node;
    }

    /// <summary>
    /// Check whether a conversion from one type to another will be made implicitly by the datasync server.
    /// </summary>
    /// <param name="node">The conversion expression</param>
    /// <param name="from">The type to convert from</param>
    /// <param name="to">The type to convert to</param>
    /// <returns>True if there is an implicit conversion</returns>
    internal bool IsConversionImplicit(UnaryExpression node, Type from, Type to)
        => GetTableMemberName(node.Operand, NamingPolicy) != null && ImplicitConversions.IsImplicitConversion(from, to);

    /// <summary>
    /// Checks if the provided binary expression is an enum.
    /// </summary>
    /// <param name="node">The binary expression to check</param>
    /// <param name="unaryExpression">The expression which is the enum.</param>
    /// <param name="constantExpression">The constant expression containing the enum value</param>
    /// <returns>Trye if an enum expression is found.</returns>
    internal static bool IsEnumExpression(BinaryExpression node, out UnaryExpression unaryExpression, out ConstantExpression constantExpression)
    {
        // Case 1: enum on the left side
        if (node.Left is UnaryExpression left && IsEnumExpression(left) && node.Right is ConstantExpression leftExpr)
        {
            unaryExpression = left;
            constantExpression = leftExpr;
            return true;
        }

        // Case 2: enum on the right side
        if (node.Right is UnaryExpression right && IsEnumExpression(right) && node.Left is ConstantExpression rightExpr)
        {
            unaryExpression = right;
            constantExpression = rightExpr;
            return true;
        }

        unaryExpression = null;
        constantExpression = null;
        return false;
    }

    /// <summary>
    /// Checks if the provided unary expression is an enum.
    /// </summary>
    /// <param name="expression">The expression to check</param>
    /// <returns>True if an enum.</returns>
    internal static bool IsEnumExpression(UnaryExpression expression)
        => expression.NodeType == ExpressionType.Convert && expression.Operand.Type.GetTypeInfo().IsEnum;

    /// <summary>
    /// Each <see cref="QueryNode"/> has a <see cref="QueryNode.SetChildren(IList{QueryNode})"/> method.  This
    /// copies the children from the filter expression stack into the query node.
    /// </summary>
    /// <param name="parent">The parent query node.</param>
    internal void SetChildren(QueryNode parent)
    {
        Stack<QueryNode> args = new();
        while (FilterExpression.Peek() != parent)
        {
            args.Push(FilterExpression.Pop());
        }

        parent.SetChildren([.. args]);
    }
}
