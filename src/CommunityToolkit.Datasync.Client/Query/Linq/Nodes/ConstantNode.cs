// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// A <see cref="QueryNode"/> representing a primitive constant value.
/// </summary>
/// <param name="value">The value of the constant</param>
internal class ConstantNode(object value) : QueryNode
{
    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.Constant;

    /// <summary>
    /// The value of the primitive constant.
    /// </summary>
    internal object Value { get; set; } = value;

    /// <inheritdoc />
    internal override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);
}
