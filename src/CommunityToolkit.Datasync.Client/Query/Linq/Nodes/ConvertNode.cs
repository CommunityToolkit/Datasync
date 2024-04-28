// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// A <see cref="QueryNode"/> representing a conversion from one type to another.
/// </summary>
/// <param name="source">The source expression that is being converted.</param>
/// <param name="targetType">The type that the source is being converted to.</param>
internal class ConvertNode(QueryNode source, Type targetType) : QueryNode
{
    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.Convert;

    /// <summary>
    /// The source value to convert
    /// </summary>
    internal QueryNode Source { get; set; } = source;

    /// <summary>
    /// The type that we're converting to
    /// </summary>
    internal Type TargetType { get; set; } = targetType;

    /// <inheritdoc/>
    /// <remarks>
    /// This Accept method is never called.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        Ensure.That(children.Count, nameof(children)).IsGt(0);
        Source = children[0];
    }
}
