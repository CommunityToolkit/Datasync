// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// The structural elements of a compiled LINQ query that can be represented 
/// by the subset of OData that a datasync service uses.
/// </summary>
internal class QueryDescription
{
    /// <summary>
    /// Creates a new blank <see cref="QueryDescription"/>
    /// </summary>
    internal QueryDescription()
    {
    }

    /// <summary>
    /// Creates a new <see cref="QueryDescription"/> based on a source <see cref="QueryDescription"/>.
    /// </summary>
    /// <param name="source">The source of the <see cref="QueryDescription"/></param>
    internal QueryDescription(QueryDescription source)
    {
        Filter = source.Filter; // Note: we don't clone the filter, so you have to be careful to not change any nodes.
        RequestTotalCount = source.RequestTotalCount;
        Ordering = [..source.Ordering];
        ProjectionArgumentType = source.ProjectionArgumentType;
        Projections = [..source.Projections];
        QueryParameters = new Dictionary<string, string>(source.QueryParameters);
        Selection = new List<string>(source.Selection);
        Skip = source.Skip;
        Top = source.Top;
    }

    /// <summary>
    /// The <see cref="QueryNode"/> for the query filter expression.
    /// </summary>
    public QueryNode Filter { get; set; }

    /// <summary>
    /// If <c>true</c>, include the total count of items that will be returned with this query
    /// (without considering paging).
    /// </summary>
    public bool RequestTotalCount { get; set; }

    /// <summary>
    /// The list of expressions that specify the ordering constraints requested by the query.
    /// </summary>
    public List<OrderByNode> Ordering { get; } = [];

    /// <summary>
    /// The type of the argument to the projection (i.e. the type that should be deserialized).
    /// </summary>
    internal Type ProjectionArgumentType { get; set; }

    /// <summary>
    /// The collection of projections that should be applied to each element of the query.
    /// </summary>
    internal List<Delegate> Projections { get; } = [];

    /// <summary>
    /// The additional user-defined query parameters to add to the query string.
    /// </summary>
    internal IDictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The list of fields that should be selected from the items in the table.
    /// </summary>
    public IList<string> Selection { get; } = [];

    /// <summary>
    /// The number of elements to skip
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// The number of elements to take.
    /// </summary>
    public int? Top { get; set; }
}
