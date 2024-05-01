// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// The ordering direction possibilities.
/// </summary>
internal enum OrderByDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Represents an $orderBy query option
/// </summary>
/// <param name="expression">The OData expression for the ordering.</param>
/// <param name="direction">The sort direction.</param>
internal class OrderByNode(QueryNode expression, OrderByDirection direction)
{
    /// <summary>
    /// Helper constructor to set ascending/descending property on boolean
    /// </summary>
    /// <param name="expression">The OData expression</param>
    /// <param name="ascending">True if ordering is ascending</param>
    internal OrderByNode(QueryNode expression, bool ascending)
        : this(expression, ascending ? OrderByDirection.Ascending : OrderByDirection.Descending)
    {
    }

    internal QueryNode Expression { get; } = expression;

    internal OrderByDirection Direction { get; } = direction;
}
