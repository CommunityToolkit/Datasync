// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Common;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

// There are a lot of unused expression values when doing tree walking, so we suppress those warnings.
#pragma warning disable IDE0058 // Expression value is never used

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// Compiles a LINQ expression tree into a <see cref="QueryDescription"/> that can
/// be executed on the server or turned into other forms.
/// </summary>
/// <remarks>
/// This code is based on the concepts discussed in <see href="http://blogs.msdn.com/b/mattwar/archive/2008/11/18/linq-links.aspx"/>.
/// </remarks>
/// <typeparam name="T">The type of the model used in the query.</typeparam>
internal class QueryTranslator<T>
{
    internal QueryTranslator(IODataQuery<T> query)
    {
        Ensure.That(query, nameof(query)).IsNotNull();
        Query = query;
        QueryDescription = new() { IncludeTotalCount = query.RequestTotalCount };
    }

    /// <summary>
    /// The query which is being translated.
    /// </summary>
    internal IODataQuery<T> Query { get; }

    /// <summary>
    /// The compiled query description generated from the expression tree.
    /// </summary>
    internal QueryDescription QueryDescription { get; }

    /// <summary>
    /// Translate an expression tree into a compiled query description that can be
    /// turned into whatever form is required.
    /// </summary>
    /// <returns>A compiled query description</returns>
    public QueryDescription Translate()
    {
        Expression expression = PartiallyEvaluate(Query.Query.Expression);
        if (expression is MethodCallExpression methodCall)
        {
            VisitMethodCall(methodCall);
        }

        QueryDescription.ProjectionArgumentType ??= typeof(T);
        return QueryDescription;
    }

    /// <summary>
    /// Determines if the provided expression is a valid LambdaExpression.
    /// </summary>
    /// <param name="expression">The expression</param>
    /// <param name="lambdaExpression">The lambda expression equivalent</param>
    /// <returns>True if a lambda expression</returns>
    internal bool ExpressionIsValidLambdaExpression(MethodCallExpression expression, out LambdaExpression lambdaExpression)
    {
        lambdaExpression = null;
        if (expression?.Arguments.Count >= 2 && StripQuote(expression.Arguments[1]) is LambdaExpression lambda)
        {
            lambdaExpression = lambda;
        }

        return lambdaExpression != null;
    }

    /// <summary>
    /// Walk the expression and compute all the subtrees that are not dependent on any
    /// of the expressions parameters.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>A collection of all the expression subtrees that are independent from the expression parameters.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method is in a generic class without type markings.")]
    internal List<Expression> FindIndependentSubtrees(Expression expression)
    {
        List<Expression> subtrees = [];
        bool dependent = false;
        bool isMemberInit = false;

        VisitorHelper.VisitAll(expression, (expr, recurse) =>
        {
            if (expr != null)
            {
                bool parentIsDependent = dependent;
                bool parentIsMemberInit = isMemberInit;
                dependent = false;
                isMemberInit = expr is MemberInitExpression;

                recurse(expr);
                if (!dependent)
                {
                    if (expr is NewExpression newExpression && parentIsMemberInit)
                    {
                        return expr;
                    }

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
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method is in a generic class without type markings.")]
    internal IEnumerable<MemberExpression> GetMemberExpressions(Expression expression)
    {
        List<MemberExpression> members = [];
        VisitorHelper.VisitMembers(expression, (expr, recurse) =>
        {
            members.Add(expr);
            return recurse(expr);
        });
        return members;
    }

    /// <summary>
    /// Evaluate all subtrees of an expression that aren't dependent on parameters to
    /// that expression and replace the subtree with a constant expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate</param>
    /// <returns>The partially evaluated expression</returns>
    internal Expression PartiallyEvaluate(Expression expression)
    {
        List<Expression> subtrees = FindIndependentSubtrees(expression);
        return VisitorHelper.VisitAll(expression, (expr, recurse) =>
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
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method is in a generic class without type markings.")]
    internal Expression StripQuote(Expression expression)
        => expression.NodeType == ExpressionType.Quote ? ((UnaryExpression)expression).Operand : expression;

    /// <summary>
    /// Process the core LINQ operators that are supported by a datasync service using
    /// a visitor process.
    /// </summary>
    /// <param name="expression">The expression to visit.</param>
    /// <returns>The visited expression.</returns>
    internal Expression VisitMethodCall(MethodCallExpression expression)
    {
        if (expression.Arguments.Count >= 1)
        {
            Expression firstArgument = expression.Arguments[0];
            if (firstArgument is MethodCallExpression methodCall && firstArgument.NodeType == ExpressionType.Call)
            {
                VisitMethodCall(methodCall);
            }
        }

        switch (expression.Method.Name)
        {
            case "OrderBy":
                AddOrdering(expression, ascending: true, prepend: true);
                break;
            case "OrderByDescending":
                AddOrdering(expression, ascending: false, prepend: true);
                break;
            case "Select":
                AddProjection(expression);
                break;
            case "Skip":
                int skipCount = GetCountArgument(expression);
                QueryDescription.Skip = QueryDescription.Skip.HasValue ? QueryDescription.Skip.Value + skipCount : skipCount;
                break;
            case "Take":
                int takeCount = GetCountArgument(expression);
                QueryDescription.Top = QueryDescription.Top.HasValue ? Math.Min(QueryDescription.Top.Value, takeCount) : takeCount;
                break;
            case "ThenBy":
                AddOrdering(expression, ascending: true, prepend: false);
                break;
            case "ThenByDescending":
                AddOrdering(expression, ascending: false, prepend: false);
                break;
            case "Where":
                AddFilter(expression);
                break;
            default:
                throw new NotSupportedException($"'{expression.Method.Name}' caluse in query expression is not supported.");
        }

        return expression;
    }

    /// <summary>
    /// Add a filtering expression to the query.
    /// </summary>
    /// <param name="expression">A Where method call expression.</param>
    internal void AddFilter(MethodCallExpression expression)
    {
        if (ExpressionIsValidLambdaExpression(expression, out LambdaExpression lambda))
        {
            QueryNode filter = FilterBuildingExpressionVisitor.Compile(lambda!.Body);
            if (QueryDescription.Filter != null)
            {
                QueryDescription.Filter = new BinaryOperatorNode(BinaryOperatorKind.And, QueryDescription.Filter, filter);
            }
            else
            {
                QueryDescription.Filter = filter;
            }

            return;
        }

        throw new NotSupportedException("'Where' clause in query expression contains in invalid predicate");
    }

    /// <summary>
    /// Add an ordering expression to the query
    /// </summary>
    /// <param name="expression">An ordering method call expression</param>
    /// <param name="ascending">True if the ordering is ascending, false otherwise</param>
    /// <param name="prepend">True to prepend the ordering to the list</param>
    internal void AddOrdering(MethodCallExpression expression, bool ascending, bool prepend)
    {
        // We only allow keySelectors that are x => x.member expressions (i.e. MemberAccessNode).
        // Anything else will result in a NotSupportedException
        if (ExpressionIsValidLambdaExpression(expression, out LambdaExpression lambda) && lambda!.Body is MemberExpression memberExpression)
        {
            string memberName = FilterBuildingExpressionVisitor.GetMemberName(memberExpression);
            if (memberName != null)
            {
                OrderByNode node = new(new MemberAccessNode(null, memberName), ascending);
                if (prepend)
                {
                    QueryDescription.Ordering.Insert(0, node);
                }
                else
                {
                    QueryDescription.Ordering.Add(node);
                }
            }
        }
        else
        {
            throw new NotSupportedException($"'{expression?.Method.Name}' query expressions must consist of members only.");
        }
    }

    /// <summary>
    /// Add a projection to the query
    /// </summary>
    /// <param name="expression">A Select Method Call expression</param>
    internal void AddProjection(MethodCallExpression expression)
    {
        // We only allow projections consisting of Select(x => ...).  Anything else throws a NotSupportedException
        if (ExpressionIsValidLambdaExpression(expression, out LambdaExpression lambda) && lambda!.Parameters.Count == 1)
        {
            QueryDescription.Projections.Add(lambda.Compile());
            if (QueryDescription.ProjectionArgumentType == null)
            {
                QueryDescription.ProjectionArgumentType = lambda.Parameters[0].Type;
                foreach (MemberExpression memberExpression in GetMemberExpressions(lambda.Body))
                {
                    string memberName = FilterBuildingExpressionVisitor.GetMemberName(memberExpression);
                    if (memberName != null)
                    {
                        QueryDescription.Selection.Add(memberName);
                    }
                }

                // Make sure we also include all the members that would be required for deserialization
                // To do this, we get all the properties on the type that has the [Required] attribute
                foreach (PropertyInfo property in typeof(T).GetProperties().Where(p => p.GetCustomAttribute<RequiredAttribute>() != null))
                {
                    string jsonPropName = FilterBuildingExpressionVisitor.JsonPropertyName(property);
                    if (!QueryDescription.Selection.Contains(jsonPropName))
                    {
                        QueryDescription.Selection.Add(jsonPropName);
                    }
                }
            }

            return;
        }

        throw new NotSupportedException("Invalid projection in 'Select' query expression");
    }

    /// <summary>
    /// Gets the count argument value for a <c>Skip</c> or <c>Take</c> method call.
    /// </summary>
    /// <param name="expression">The method call expression.</param>
    /// <returns>The count argument</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Static in a generic class needs generic marker, which this doesn't have.")]
    internal int GetCountArgument(MethodCallExpression expression)
    {
        Expression deepest = expression;
        if (expression?.Arguments.Count >= 2 && expression.Arguments[1] is ConstantExpression constant)
        {
            deepest = constant;
            if (constant.Value is int @int)
            {
                return @int;
            }
        }

        throw new NotSupportedException($"'{expression?.Method?.Name}' query expressions must consist of a single integer, not '{deepest?.ToString()}'.");
    }
}
