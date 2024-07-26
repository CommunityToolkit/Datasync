// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

internal class QueryTranslator<TEntity>
{
    internal QueryTranslator(IDatasyncQueryable<TEntity> query)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        Query = query;
        QueryDescription = new() { RequestTotalCount = query.RequestTotalCount };
    }

    /// <summary>
    /// The queryable that this translator is converting.
    /// </summary>
    internal IDatasyncQueryable<TEntity> Query { get; }

    /// <summary>
    /// The compiled query description for the queryable.
    /// </summary>
    internal QueryDescription QueryDescription { get; }

    internal QueryDescription Translate()
    {
        var expression = Query.Queryable.Expression.PartiallyEvaluate();
        if (expression is MethodCallExpression mce)
        {
            VisitMethodCall(mce);
        }

        QueryDescription.ProjectionArgumentType ??= typeof(TEntity);
        return QueryDescription;
    }
}
