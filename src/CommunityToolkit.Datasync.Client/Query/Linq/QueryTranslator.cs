// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using System.Linq.Expressions;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// Compiles a LINQ expression tree into a <see cref="QueryDescription"/> that can
/// be executed on the server or turned into other forms.
/// </summary>
/// <remarks>
/// This code is based on the concepts discussed in <see href="http://blogs.msdn.com/b/mattwar/archive/2008/11/18/linq-links.aspx"/>.
/// </remarks>
/// <typeparam name="T">The type of the model used in the query.</typeparam>
internal class QueryTranslator<T> where T : class
{
    /// <summary>
    /// Creates a new <see cref="QueryTranslator{T}"/> based on the provided query.
    /// </summary>
    /// <param name="query">The <see cref="IDatasyncQueryable{TEntity}"/> to translate.</param>
    internal QueryTranslator(IDatasyncQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        Query = query.Queryable;
        QueryDescription = new() { QueryParameters = query.QueryParameters, RequestTotalCount = query.RequestTotalCount };
        NamingPolicy = ((DatasyncServiceClient<T>)query.ServiceClient).JsonSerializerOptions.PropertyNamingPolicy;
    }

    /// <summary>
    /// Creates a new <see cref="QueryTranslator{T}"/> based on the provided query.
    /// </summary>
    /// <param name="query">The <see cref="IDatasyncPullQuery{TEntity}"/> to translate.</param>
    internal QueryTranslator(IDatasyncPullQuery<T> query)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        Query = query.Queryable;
        QueryDescription = new() { QueryParameters = query.QueryParameters, RequestTotalCount = false };
        NamingPolicy = DatasyncSerializer.JsonSerializerOptions.PropertyNamingPolicy;
    }

    /// <summary>
    /// The source query.
    /// </summary>
    internal IQueryable<T> Query { get; }

    /// <summary>
    /// The destination query description.
    /// </summary>
    internal QueryDescription QueryDescription { get; }

    /// <summary>
    /// The naming policy to use in converting a member into an OData property.
    /// </summary>
    internal JsonNamingPolicy NamingPolicy { get; }

    /// <summary>
    /// Replaceable function for GetTableMemberName.
    /// </summary>
    internal Func<MemberExpression, JsonNamingPolicy, string> GetTableMemberName { get; set; } = DefaultGetTableMemberName;

    /// <summary>
    /// Translates an expression tree into a compiled query description that can be turned into
    /// whatever form is required.
    /// </summary>
    /// <returns>A compiled <see cref="QueryDescription"/> object.</returns>
    internal QueryDescription Translate()
    {
        Expression expression = Query.Expression.PartiallyEvaluate();
        if (expression is MethodCallExpression methodCall)
        {
            VisitMethodCall(methodCall);
        }

        QueryDescription.ProjectionArgumentType ??= typeof(T);
        return QueryDescription;
    }

    /// <summary>
    /// Process the core LINQ operators that are supported by a datasync service using
    /// a visitor process.
    /// </summary>
    /// <param name="expression">The expression to visit.</param>
    /// <returns>The visited expression.</returns>
    internal void VisitMethodCall(MethodCallExpression expression)
    {
        // Recurse down the target of the method call until we get to something we need to process.
        if (expression.Arguments.Count >= 1)
        {
            Expression firstArgument = expression.Arguments[0];
            if (firstArgument is MethodCallExpression methodCall && firstArgument.NodeType == ExpressionType.Call)
            {
                VisitMethodCall(methodCall);
            }
        }

        AddByMethodName(expression, expression.Method.Name);
    }

    /// <summary>
    /// Calls the appropriate addition mechanism based on the LINQ method.
    /// </summary>
    /// <param name="expression">The expression being processed.</param>
    /// <param name="methodName">The method name.</param>
    /// <exception cref="NotSupportedException">Thrown if the method name is not supported.</exception>
    internal void AddByMethodName(MethodCallExpression expression, string methodName)
    { 
        switch (methodName)
        {
            case "OrderBy":
                AddOrdering(expression, ascending: true, prepend: true);
                break;
            case "OrderByDescending":
                AddOrdering(expression, ascending: false, prepend: true);
                break;
            case "ThenBy":
                AddOrdering(expression, ascending: true, prepend: false);
                break;
            case "ThenByDescending":
                AddOrdering(expression, ascending: false, prepend: false);
                break;

            case "Select":
                AddProjection(expression);
                break;

            case "Where":
                AddFilter(expression);
                break;

            case "Skip":
                int skipCount = QueryTranslator<T>.GetCountArgument(expression);
                QueryDescription.Skip = QueryDescription.Skip + skipCount;
                break;
            case "Take":
                int takeCount = QueryTranslator<T>.GetCountArgument(expression);
                QueryDescription.Top = QueryDescription.Top.HasValue ? Math.Min(QueryDescription.Top.Value, takeCount) : takeCount;
                break;

            default:
                throw new NotSupportedException($"'{methodName}' clause in query expression is not supported.");
        }
    }

    /// <summary>
    /// Add a filtering expression to the query.
    /// </summary>
    /// <param name="expression">A Where method call expression.</param>
    internal void AddFilter(MethodCallExpression expression)
    {
        if (expression.IsValidLambdaExpression(out LambdaExpression lambda))
        {
            QueryNode filter = FilterBuildingExpressionVisitor.Compile(lambda!.Body, NamingPolicy);
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
        if (expression.IsValidLambdaExpression(out LambdaExpression lambda) && lambda!.Body is MemberExpression memberExpression)
        {
            string memberName = GetTableMemberName(memberExpression, NamingPolicy);
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

                return;
            }
        }
        
        throw new NotSupportedException($"'{expression?.Method.Name}' query expressions must consist of members only.");
    }

    /// <summary>
    /// Add a projection to the query
    /// </summary>
    /// <param name="expression">A Select Method Call expression</param>
    internal void AddProjection(MethodCallExpression expression)
    {
        // We only allow projections consisting of Select(x => ...).  Anything else throws a NotSupportedException
        if (expression.IsValidLambdaExpression(out LambdaExpression lambda) && lambda!.Parameters.Count == 1)
        {
            QueryDescription.Projections.Add(lambda.Compile());
            if (QueryDescription.ProjectionArgumentType == null)
            {
                QueryDescription.ProjectionArgumentType = lambda.Parameters[0].Type;
                foreach (MemberExpression memberExpression in lambda.Body.GetMemberExpressions())
                {
                    string memberName = GetTableMemberName(memberExpression, NamingPolicy);
                    if (memberName != null)
                    {
                        QueryDescription.Selection.Add(memberName);
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
    /// Gets the count argument value for a <c>Skip</c> or <c>Take</c> method call.
    /// </summary>
    /// <param name="expression">The method call expression.</param>
    /// <returns>The count argument</returns>
    internal static int GetCountArgument(MethodCallExpression expression)
    {
        if (expression != null)
        {
            if (expression.Arguments.Count >= 2)
            {
                if (expression.Arguments[1] is ConstantExpression constant)
                {
                    if (constant.Value is int @int)
                    {
                        return @int;
                    }
                }
            }
        }

        throw new NotSupportedException("'Skip' and 'Take' methods must use a single integer.");
    }

    /// <summary>
    /// The default version of GetTableMemberName that calls the expression visitor version.
    /// </summary>
    /// <param name="memberExpression">The MemberExpression that needs to be translated.</param>
    /// <param name="namingPolicy">The naming policy to translate.</param>
    /// <returns></returns>
    internal static string DefaultGetTableMemberName(MemberExpression memberExpression, JsonNamingPolicy namingPolicy)
        => FilterBuildingExpressionVisitor.GetTableMemberName(memberExpression, namingPolicy);
}
