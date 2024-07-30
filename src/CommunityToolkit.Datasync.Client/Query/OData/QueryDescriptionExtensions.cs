// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;

namespace CommunityToolkit.Datasync.Client.Query.OData;

/// <summary>
/// A set of extension methods to enable OData translation for a <see cref="QueryDescription"/>.
/// </summary>
internal static class QueryDescriptionExtensions
{
    /// <summary>
    /// Converts the query structure into a standard OData URI protocol for queries.
    /// </summary>
    /// <param name="value">The <see cref="QueryDescription"/> to convert to OData.</param>
    /// <returns>A URI query string representing the query.</returns>
    public static string ToODataQueryString(this QueryDescription value)
    {
        List<string> queryFragments = [];

        if (value.Filter != null)
        {
            string filterStr = ODataExpressionVisitor.ToODataQueryString(value.Filter);
            queryFragments.Add($"{ODataQueryParameters.Filter}={Uri.EscapeDataString(filterStr)}");
        }

        if (value.Ordering.Count > 0)
        {
            IEnumerable<string> orderings = value.Ordering.Select(o => o.ToODataQueryString());
            queryFragments.Add($"{ODataQueryParameters.OrderBy}={string.Join(",", orderings)}");
        }

        if (value.Skip > 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Skip}={value.Skip}");
        }

        if (value.Top.HasValue && value.Top >= 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Top}={value.Top}");
        }

        if (value.Selection.Count > 0)
        {
            queryFragments.Add($"{ODataQueryParameters.Select}={string.Join(",", value.Selection.OrderBy(field => field).Select(Uri.EscapeDataString))}");
        }

        if (value.RequestTotalCount)
        {
            queryFragments.Add($"{ODataQueryParameters.Count}=true");
        }

        foreach (string paramKey in value.QueryParameters.Keys.OrderBy(x => x))
        {
            queryFragments.Add($"{Uri.EscapeDataString(paramKey)}={Uri.EscapeDataString(value.QueryParameters[paramKey])}");
        }

        return string.Join("&", queryFragments);
    }

    /// <summary>
    /// Convert this ordering node into an OData string representation.
    /// </summary>
    /// <returns>The OData string</returns>
    public static string ToODataQueryString(this OrderByNode node)
    {
        string field = ODataExpressionVisitor.ToODataQueryString(node.Expression);
        string direction = node.Direction == OrderByDirection.Ascending ? "" : " desc";
        return $"{field}{direction}";
    }
}
