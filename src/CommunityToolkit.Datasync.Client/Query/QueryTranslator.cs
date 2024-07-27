// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Several methods can be static, but are also generic.  We have to choose which
// warning to disable, since there will always be a warning.  I've opted to make
// the methods a part of the class rather than static throughout the class.
#pragma warning disable CA1822 // Mark members as static

using CommunityToolkit.Datasync.Client.Service;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Query;

internal class QueryTranslator<TEntity>
{
    internal QueryTranslator(IDatasyncQueryable<TEntity> query)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        Query = query;
        JsonSerializerOptions = ((DatasyncServiceClient<TEntity>)query.ServiceClient).JsonSerializerOptions;
        QueryDescription = new()
        {
            RequestTotalCount = query.RequestTotalCount,
            QueryParameters = query.QueryParameters
        };
    }

    /// <summary>
    /// The queryable that this translator is converting.
    /// </summary>
    internal IDatasyncQueryable<TEntity> Query { get; }

    /// <summary>
    /// The JSON serializer options being used for this request.
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// The compiled query description for the queryable.
    /// </summary>
    internal QueryDescription QueryDescription { get; }

    /// <summary>
    /// Translates the provided query into a <see cref="QueryDescription"/>.
    /// </summary>
    /// <returns>The <see cref="QueryDescription"/> that matches the <see cref="Query"/> that is provided.</returns>
    internal QueryDescription Translate()
    {
        Expression expression = PartiallyEvaluate(Query.Queryable.Expression);
        if (expression is MethodCallExpression mce)
        {
            _ = VisitMethodCall(mce);
        }

        QueryDescription.ProjectionArgumentType ??= typeof(TEntity);
        return QueryDescription;
    }

    /// <summary>
    /// Add a filtering expression to the query.
    /// </summary>
    /// <param name="expression">A Where method call expression.</param>
    internal protected void AddFilter(MethodCallExpression expression)
    {
        if (IsValidLambdaExpression(expression, out LambdaExpression? lambda))
        {
            QueryNode filter = FilterBuildingExpressionVisitor.Compile(lambda.Body, JsonSerializerOptions);
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
    internal protected void AddOrdering(MethodCallExpression expression, bool ascending, bool prepend)
    {
        // We only allow keySelectors that are x => x.member expressions (i.e. MemberAccessNode).
        // Anything else will result in a NotSupportedException
        if (IsValidLambdaExpression(expression, out LambdaExpression? lambda) && lambda.Body is MemberExpression memberExpression)
        {
            string memberName = FilterBuildingExpressionVisitor.GetTableMemberName(memberExpression, JsonSerializerOptions);
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
    internal protected void AddProjection(MethodCallExpression expression)
    {
        // We only allow projections consisting of Select(x => ...).  Anything else throws a NotSupportedException
        if (IsValidLambdaExpression(expression, out LambdaExpression? lambda) && lambda.Parameters.Count == 1)
        {
            QueryDescription.Projections.Add(lambda.Compile());
            if (QueryDescription.ProjectionArgumentType == null)
            {
                QueryDescription.ProjectionArgumentType = lambda.Parameters[0].Type;
                foreach (MemberExpression memberExpression in GetMemberExpressions(lambda.Body))
                {
                    string memberName = FilterBuildingExpressionVisitor.GetTableMemberName(memberExpression, JsonSerializerOptions);
                    if (string.IsNullOrEmpty(memberName))
                    {
                        QueryDescription.Selections.Add(memberName);
                    }
                }

                // TODO: Make sure we also include all the members that would be required for deserialization
                //JsonContract contract = ContractResolver.ResolveContract(QueryDescription.ProjectionArgumentType);
                //if (contract is JsonObjectContract objectContract)
                //{
                //    foreach (string propName in objectContract.Properties.Where(p => p.Required == Required.Always || p.Required == Required.AllowNull).Select(p => p.PropertyName))
                //    {
                //        if (!QueryDescription.Selection.Contains(propName))
                //        {
                //            QueryDescription.Selection.Add(propName);
                //        }
                //    }
                //}
            }

            return;
        }

        throw new NotSupportedException("Invalid projection in 'Select' query expression");
    }

    /// <summary>
    /// Walk the expression and compute all the subtrees that are not dependent on any
    /// of the expressions parameters.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>A collection of all the expression subtrees that are independent from the expression parameters.</returns>
    internal List<Expression> FindIndependentSubtrees(Expression expression)
    {
        List<Expression> subtrees = [];

        // The dependent and isMemberInit flags are used to communicate between different layers of the recursive visitor.
        bool dependent = false;
        bool isMemberInit = false;

        // Walk the tree, finding the independent subtrees
        _ = VisitorHelper.VisitAll(expression, (Expression expr, Func<Expression, Expression> recurse) =>
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
                // A NewExpression itself will appear to be indepentant, but if the parent is a MemberInitExpression,
                // then the NewExpression can't be evaluated by itself.  The MemberInitExpression will determine
                // if the full expression is dependent or not, so don't check it here.
                if (expr is NewExpression newExpression && parentIsMemberInit)
                {
                    return expr;
                }

                // The current node is independent if it's not related to the parameter and it's not the constant query root.
                if (expr.NodeType == ExpressionType.Parameter || (expr is ConstantExpression constantExpr && constantExpr.Value is IQueryable))
                {
                    dependent = true;
                }
                else
                {
                    subtrees.Add(expr);
                }
            }

            dependent |= parentIsDependent;
            return expr;
        });
        return subtrees;
    }

    /// <summary>
    /// Gets the count argument value for a <c>Skip</c> or <c>Take</c> method call.
    /// </summary>
    /// <param name="expression">The method call expression.</param>
    /// <returns>The count argument</returns>
    internal protected int GetCountArgument(MethodCallExpression expression)
    {
        Expression deepest = expression;

        // We only allow Skip(x) expressions.  Anything else will result in an exception.
        if (expression.Arguments.Count >= 2 && expression.Arguments[1] is ConstantExpression constant)
        {
            deepest = constant;
            if (constant.Value is int @int)
            {
                return @int;
            }
        }

        throw new NotSupportedException($"'{expression.Method.Name}' query expressions must consist of a single integer, not '{deepest}'.");
    }

    /// <summary>
    /// Returns the member expressions in the expression hierarchy of the <paramref name="expression"/>
    /// </summary>
    /// <param name="expression">The expression to search</param>
    /// <returns>A collection of <see cref="MemberExpression"/> objects</returns>
    internal IEnumerable<MemberExpression> GetMemberExpressions(Expression expression)
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
    internal static bool IsValidLambdaExpression(MethodCallExpression expression, [NotNullWhen(true)] out LambdaExpression? lambdaExpression)
    {
        lambdaExpression = null;
        if (expression?.Arguments.Count >= 2 && StripQuote(expression.Arguments[1]) is LambdaExpression lambda)
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
    internal Expression PartiallyEvaluate(Expression expression)
    {
        List<Expression> subtrees = FindIndependentSubtrees(expression);
        return VisitorHelper.VisitAll(expression, (Expression expr, Func<Expression, Expression> recurse) =>
        {
            if (subtrees.Contains(expr) && expr.NodeType != ExpressionType.Constant)
            {
                Delegate compiled = Expression.Lambda(expr).Compile();
                object value = compiled.DynamicInvoke()!;
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
    internal static Expression StripQuote(Expression expression)
        => expression.NodeType == ExpressionType.Quote ? ((UnaryExpression)expression).Operand : expression;

    /// <summary>
    /// Process the core LINQ operators that are supported by a datasync service using
    /// a visitor process.
    /// </summary>
    /// <param name="expression">The expression to visit.</param>
    /// <returns>The visited expression.</returns>
    internal protected Expression VisitMethodCall(MethodCallExpression expression)
    {
        // Recurse down the target of the method call until we get to something we need to process.
        if (expression.Arguments.Count >= 1)
        {
            Expression firstArgument = expression.Arguments[0];
            if (firstArgument is MethodCallExpression methodCall && firstArgument.NodeType == ExpressionType.Call)
            {
                _ = VisitMethodCall(methodCall);
            }
        }

        // Handle the method call itself.  There is only a certain list of LINQ method calls that we handle.
        // Note that Skip(), Take(), and non-standard LINQ calls are handled elsewhere.
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
                QueryDescription.Skip = QueryDescription.Skip + skipCount;
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
}
