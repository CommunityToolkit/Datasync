// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;

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
/// <param name="expression">An expression that is being used for the ordering.</param>
/// <param name="direction">The direction of the ordering.</param>
internal class OrderByNode(QueryNode expression, OrderByDirection direction)
{
    /// <summary>
    /// A helper that converts a boolean into an ascending / descening direction.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="ascending"></param>
    internal OrderByNode(QueryNode expression, bool ascending) : this(expression, ascending ? OrderByDirection.Ascending : OrderByDirection.Descending)
    {
    }

    /// <summary>
    /// The expression that is being used for the ordering.
    /// </summary>
    internal QueryNode Expression { get; } = expression;

    /// <summary>
    /// A direction for the ordering.
    /// </summary>
    internal OrderByDirection Direction { get; } = direction;
}
