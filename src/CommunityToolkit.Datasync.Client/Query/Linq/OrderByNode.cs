// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// The ordering direction possibilities.
/// </summary>
internal enum OrderByDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Represents a single element in the $orderBy query option
/// </summary>
/// <remarks>
/// Creates a new <see cref="OrderByNode"/> with an expression and direction
/// </remarks>
/// <param name="expression">The OData expression.</param>
/// <param name="direction">The ordering direction.</param>
internal class OrderByNode(QueryNode expression, OrderByDirection direction)
{
    /// <summary>
    /// Helper constructor to set ascending/descending property on boolean.
    /// </summary>
    /// <param name="expression">The OData expression.</param>
    /// <param name="ascending">True if ordering is ascending.</param>
    public OrderByNode(QueryNode expression, bool ascending)
        : this(expression, ascending ? OrderByDirection.Ascending : OrderByDirection.Descending)
    {
    }

    /// <summary>
    /// The OData expression for the ordering.
    /// </summary>
    public QueryNode Expression { get; } = expression;

    /// <summary>
    /// The direction for the ordering.
    /// </summary>
    public OrderByDirection Direction { get; } = direction;
}
