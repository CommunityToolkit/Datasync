// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// A set of extensions methods that help to deal with <see cref="Expression"/> values,
/// which are used extensively in LINQ parsing.
/// </summary>
internal static class ExpressionExtensions
{
    /// <summary>
    /// Walk the expression and compute all the subtrees that are not dependent on any
    /// of the expressions parameters.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>A collection of all the expression subtrees that are independent from the expression parameters.</returns>
    internal static List<Expression> FindIndependentSubtrees(this Expression expression)
    {
        List<Expression> subtrees = [];

        // The dependenty and isMemberInit flags are used to communicate between different layers
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
                    // A NewExpression itself will appear to be indepentt, but if the parent is a MemberInitExpression,
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
    internal static bool IsValidLambdaExpression(this MethodCallExpression expression, out LambdaExpression lambdaExpression)
    {
        lambdaExpression = null;
        if (expression?.Arguments.Count >= 2 && expression.Arguments[1].StripQuote() is LambdaExpression lambda)
        {
            lambdaExpression = lambda;
        }

        return lambdaExpression != null;
    }

    /// <summary>
    /// Evaluate all subtrees of an expression that aren't dependent on parameters to
    /// that expression and replace the subtree with a constant expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate</param>
    /// <returns>The partially evaluated expression</returns>
    internal static Expression PartiallyEvaluate(this Expression expression)
    {
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

    /// <summary>
    /// Remove the quote from quoted expressions.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>An unquoted expression</returns>
    internal static Expression StripQuote(this Expression expression)
        => expression.NodeType == ExpressionType.Quote ? ((UnaryExpression)expression).Operand : expression;
}
