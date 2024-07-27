// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// The structural elements of a compiled LINQ query that can be represented by
/// the subset of OData v4 that a datasync service uses.
/// </summary>
internal class QueryDescription
{
    /// <summary>
    /// The <see cref="QueryNode"/> for the root of the filter expression.
    /// </summary>
    internal QueryNode? Filter { get; set; }

    /// <summary>
    /// The list of expressions that specify the ordering constraints requested by the query.
    /// </summary>
    internal List<OrderByNode> Ordering { get; set; } = [];

    /// <summary>
    /// Tye type of the argument to the projection (i.e. the type that should be deserialized)
    /// </summary>
    internal Type? ProjectionArgumentType { get; set; }

    /// <summary>
    /// The collection of projections that should be applied to each element of the query.
    /// </summary>
    internal List<Delegate> Projections { get; set; } = [];

    /// <summary>
    /// The additional user-defined query parameters to send along with the query string.
    /// </summary>
    internal IDictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// If <c>true</c>, requests that the total count of items that will be returned with this query.
    /// </summary>
    internal bool RequestTotalCount { get; set; }

    /// <summary>
    /// The list of selected fields that should be requested from the server.
    /// </summary>
    internal List<string> Selections { get; set; } = [];

    /// <summary>
    /// The number of elements to skip within the query set.
    /// </summary>
    internal int Skip { get; set; } = 0;

    /// <summary>
    /// The number of elements to take within the query set.
    /// </summary>
    internal int? Top { get; set; }

    /// <summary>
    /// Converts the provided query description into an OData query string.
    /// </summary>
    /// <returns>An OData v4 query string.</returns>
    /// <summary>
    /// Converts the query structure into a standard OData URI protocol for queries.
    /// </summary>
    /// <returns>A URI query string representing the query.</returns>
    public string ToODataQueryString()
    {
        List<string> queryFragments = [];

        if (Filter != null)
        {
            queryFragments.Add($"{ODataQueryParameters.Filter}={Uri.EscapeDataString(ODataExpressionVisitor.ToODataString(Filter))}");
        }

        if (Ordering.Count > 0)
        {
            queryFragments.Add($"{ODataQueryParameters.OrderBy}={string.Join(",", Ordering.Select(o => o.ToODataString()))}");
        }

        if (Skip >= 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Skip}={Skip}");
        }

        if (Top.HasValue && Top >= 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Top}={Top}");
        }

        if (Selections.Count > 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Select}={string.Join(",", Selections.OrderBy(field => field).Select(Uri.EscapeDataString))}");
        }

        if (RequestTotalCount)
        {
            queryFragments.Add($"{ODataQueryParameters.Count}=true");
        }

        foreach (KeyValuePair<string, string> kv in QueryParameters)
        {
            queryFragments.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
        }

        return string.Join("&", queryFragments);
    }
}
