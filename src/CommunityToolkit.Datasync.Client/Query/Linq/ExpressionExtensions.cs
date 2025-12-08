// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// A set of extensions methods that help to deal with <see cref="Expression"/> values,
/// which are used extensively in LINQ parsing.
/// </summary>
internal static class ExpressionExtensions
{
    private static readonly MethodInfo Contains;
    private static readonly MethodInfo SequenceEqual;

    static ExpressionExtensions()
    {
        Dictionary<string, List<MethodInfo>> queryableMethodGroups = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .GroupBy(mi => mi.Name)
            .ToDictionary(e => e.Key, l => l.ToList());

        MethodInfo GetMethod(string name, int genericParameterCount, Func<Type[], Type[]> parameterGenerator)
            => queryableMethodGroups[name].Single(mi => ((genericParameterCount == 0 && !mi.IsGenericMethod)
                    || (mi.IsGenericMethod && mi.GetGenericArguments().Length == genericParameterCount))
                && mi.GetParameters().Select(e => e.ParameterType).SequenceEqual(
                    parameterGenerator(mi.IsGenericMethod ? mi.GetGenericArguments() : [])));

        Contains = GetMethod(
            nameof(Enumerable.Contains), 1,
            types => [typeof(IEnumerable<>).MakeGenericType(types[0]), types[0]]);
        SequenceEqual = GetMethod(
            nameof(Enumerable.SequenceEqual), 1,
            types => [typeof(IEnumerable<>).MakeGenericType(types[0]), typeof(IEnumerable<>).MakeGenericType(types[0])]);
    }

    /// <summary>
    /// Walk the expression and compute all the subtrees that are not dependent on any
    /// of the expressions parameters.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>A collection of all the expression subtrees that are independent from the expression parameters.</returns>
    internal static List<Expression> FindIndependentSubtrees(this Expression expression)
    {
        List<Expression> subtrees = [];

        // The dependent and isMemberInit flags are used to communicate between different layers
        // of the recursive visitor.
        bool dependent = false;
        bool isMemberInit = false;

        // Walk the tree, finding the independent subtrees
        _ = VisitorHelper.VisitAll(expression, (Expression expr, Func<Expression, Expression> recurse) =>
        {
            if (expr != null)
            {
                bool parentIsDependent = dependent;
                bool parentIsMemberInit = isMemberInit;

                // Set flags
                dependent = false;
                isMemberInit = expr is MemberInitExpression;

                // Recurse
                _ = recurse(expr);

                // If nothing in my subtree is dependent
                if (!dependent)
                {
                    // A NewExpression itself will appear to be independent, but if the parent is a MemberInitExpression,
                    // then the NewExpression can't be evaluated by itself.  The MemberInitExpression will determine
                    // if the full expression is dependent or not, so don't check it here.
                    if (expr is NewExpression newExpression && parentIsMemberInit)
                    {
                        return expr;
                    }

                    // The current node is independent if it's not related to the parameter and it's not the constant query root.
                    ConstantExpression constant = expr as ConstantExpression;
                    if (expr.NodeType == ExpressionType.Parameter || (constant?.Value is IQueryable))
                    {
                        dependent = true;
                    }
                    else
                    {
                        subtrees.Add(expr);
                    }
                }

                dependent |= parentIsDependent;
            }

            return expr;
        });

        return subtrees;
    }

    /// <summary>
    /// Returns the member expressions in the expression hierarchy of the <paramref name="expression"/>
    /// </summary>
    /// <param name="expression">The expression to search</param>
    /// <returns>A collection of <see cref="MemberExpression"/> objects</returns>
    internal static IEnumerable<MemberExpression> GetMemberExpressions(this Expression expression)
    {
        List<MemberExpression> members = [];
        _ = VisitorHelper.VisitMembers(expression, (MemberExpression expr, Func<MemberExpression, Expression> recurse) =>
        {
            members.Add(expr);
            return recurse(expr);
        });
        return members;
    }

    /// <summary>
    /// Determines if the provided expression is a valid LambdaExpression.
    /// </summary>
    /// <param name="expression">The expression</param>
    /// <param name="lambdaExpression">The lambda expression equivalent</param>
    /// <returns>True if a lambda expression</returns>
    internal static bool IsValidLambdaExpression(this MethodCallExpression expression, [NotNullWhen(true)] out LambdaExpression lambdaExpression)
    {
        if (expression != null)
        {
            if (expression.Arguments.Count >= 2)
            {
                if (expression.Arguments[1].StripQuote() is LambdaExpression lambda)
                {
                    lambdaExpression = lambda;
                    return true;
                }
            }
        }

        lambdaExpression = null;
        return false;
    }

    /// <summary>
    /// Evaluate all subtrees of an expression that aren't dependent on parameters to
    /// that expression and replace the subtree with a constant expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate</param>
    /// <returns>The partially evaluated expression</returns>
    internal static Expression PartiallyEvaluate(this Expression expression)
    {
        expression = expression.RemoveSpanImplicitCast();
        List<Expression> subtrees = expression.FindIndependentSubtrees();
        return VisitorHelper.VisitAll(expression, (Expression expr, Func<Expression, Expression> recurse) =>
        {
            if (expr != null && subtrees.Contains(expr) && expr.NodeType != ExpressionType.Constant)
            {
                Delegate compiled = Expression.Lambda(expr).Compile();
                object value = compiled.DynamicInvoke();
                return Expression.Constant(value, expr.Type);
            }
            else
            {
                return recurse(expr);
            }
        });
    }

    internal static Expression RemoveSpanImplicitCast(this Expression expression)
    {
        return VisitorHelper.VisitAll(expression, (Expression expr, Func<Expression, Expression> recurse) =>
        {
            if (expr is MethodCallExpression methodCall)
            {
                MethodInfo method = methodCall.Method;

                if (method.DeclaringType == typeof(MemoryExtensions))
                {
                    switch (method.Name)
                    {
                        case nameof(MemoryExtensions.Contains)
                            when methodCall.Arguments is [Expression arg0, Expression arg1] && TryUnwrapSpanImplicitCast(arg0, out Expression unwrappedArg0):
                        {
                            Expression unwrappedExpr = Expression.Call(
                                    Contains.MakeGenericMethod(methodCall.Method.GetGenericArguments()[0]),
                                    unwrappedArg0, arg1);
                            return recurse(unwrappedExpr);
                        }

                        case nameof(MemoryExtensions.SequenceEqual)
                            when methodCall.Arguments is [Expression arg0, Expression arg1]
                            && TryUnwrapSpanImplicitCast(arg0, out Expression unwrappedArg0)
                            && TryUnwrapSpanImplicitCast(arg1, out Expression unwrappedArg1):
                        {
                            Expression unwrappedExpr = Expression.Call(
                                    SequenceEqual.MakeGenericMethod(methodCall.Method.GetGenericArguments()[0]),
                                    unwrappedArg0, unwrappedArg1);
                            return recurse(unwrappedExpr);
                        }
                    }

                    static bool TryUnwrapSpanImplicitCast(Expression expression, out Expression result)
                    {
                        if (expression is MethodCallExpression
                            {
                                Method: { Name: "op_Implicit", DeclaringType: { IsGenericType: true } implicitCastDeclaringType },
                                Arguments: [Expression unwrapped]
                            }
                            && implicitCastDeclaringType.GetGenericTypeDefinition() is Type genericTypeDefinition
                            && (genericTypeDefinition == typeof(Span<>) || genericTypeDefinition == typeof(ReadOnlySpan<>)))
                        {
                            result = unwrapped;
                            return true;
                        }

                        result = null;
                        return false;
                    }
                }
            }

            return recurse(expr);
        });
    }

    /// <summary>
    /// Remove the quote from quoted expressions.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>An unquoted expression</returns>
    internal static Expression StripQuote(this Expression expression)
        => expression.NodeType == ExpressionType.Quote ? ((UnaryExpression)expression).Operand : expression;
}
