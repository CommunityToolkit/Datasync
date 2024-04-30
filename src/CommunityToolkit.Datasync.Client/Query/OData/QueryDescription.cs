// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

namespace CommunityToolkit.Datasync.Client.Query.OData;

/// <summary>
/// The structural elements of a compiled LINQ query that can be
/// represented by the subset of OData that a Datasync service uses.
/// </summary>
internal class QueryDescription
{
    /// <summary>
    /// The <see cref="QueryNode"/> for the query filter expression.
    /// </summary>
    public QueryNode Filter { get; set; }

    /// <summary>
    /// If <c>true</c>, include the total count of items that will be returned with this query
    /// (without considering paging).
    /// </summary>
    public bool IncludeTotalCount { get; set; }

    /// <summary>
    /// The list of expressions that specify the ordering constraints requested by the query.
    /// </summary>
    public IList<OrderByNode> Ordering { get; } = [];

    /// <summary>
    /// The type of the argument to the projection (i.e. the type that should be deserialized).
    /// </summary>
    internal Type ProjectionArgumentType { get; set; }

    /// <summary>
    /// The collection of projections that should be applied to each element of the query.
    /// </summary>
    internal List<Delegate> Projections { get; } = [];

    /// <summary>
    /// The list of fields that should be selected from the items in the table.
    /// </summary>
    public IList<string> Selection { get; } = [];

    /// <summary>
    /// The number of elements to skip
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// The number of elements to take.
    /// </summary>
    public int? Top { get; set; }

    /// <summary>
    /// Converts the query structure into a standard OData URI protocol for queries.
    /// </summary>
    /// <param name="parameters">A list of optional parameters to include in the query string.</param>
    /// <returns>A URI query string representing the query.</returns>
    public string ToODataString(IDictionary<string, string> parameters = null)
    {
        List<string> queryFragments = [];

        if (Filter != null)
        {
            string filterStr = ODataExpressionVisitor.ToODataString(Filter);
            queryFragments.Add($"{ODataQueryParameters.Filter}={Uri.EscapeDataString(filterStr)}");
        }

        if (Ordering.Count > 0)
        {
            IEnumerable<string> orderings = Ordering.Select(o => ConvertOrderByNode(o));
            queryFragments.Add($"{ODataQueryParameters.OrderBy}={string.Join(",", orderings)}");
        }

        if (Skip.HasValue && Skip >= 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Skip}={Skip}");
        }

        if (Top.HasValue && Top >= 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Top}={Top}");
        }

        if (Selection.Count > 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Select}={string.Join(",", Selection.OrderBy(field => field).Select(Uri.EscapeDataString))}");
        }

        if (IncludeTotalCount)
        {
            queryFragments.Add($"{ODataQueryParameters.InlineCount}=true");
        }

        if (parameters?.Count > 0)
        {
            foreach (KeyValuePair<string, string> kv in parameters)
            {
                queryFragments.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
            }
        }

        return string.Join("&", queryFragments);
    }

    /// <summary>
    /// Convert the <see cref="OrderByNode" /> into an OData string representation.
    /// </summary>
    /// <param name="node">The node to convert.</param>
    /// <returns>The OData string</returns>
    internal static string ConvertOrderByNode(OrderByNode node)
    {
        string field = ODataExpressionVisitor.ToODataString(node.Expression);
        string direction = node.Direction == OrderByDirection.Ascending ? "" : " desc";
        return $"{field}{direction}";
    }
}
