// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// A <see cref="QueryNode"/> representing a function call
/// </summary>
/// <param name="name">The name of the function</param>
/// <param name="arguments">The arguments to the function</param>
internal class FunctionCallNode(string name, IList<QueryNode> arguments = null) : QueryNode
{
    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.FunctionCall;

    /// <summary>
    /// The name of the function to call.
    /// </summary>
    internal string Name { get; set; } = name;

    /// <summary>
    /// The list of arguments to this function call.
    /// </summary>
    internal IList<QueryNode> Arguments { get; set; } = arguments ?? [];

    /// <inheritdoc/>
    internal override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    internal override void SetChildren(IList<QueryNode> children)
    {
        Arguments = [.. children];
    }
}
