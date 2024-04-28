// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// A <see cref="QueryNode"/> representing access to a member value
/// </summary>
/// <param name="instance">The object instance that we are accessing</param>
/// <param name="memberName">The name of the member we are accessing</param>
internal class MemberAccessNode(QueryNode instance, string memberName) : QueryNode
{
    /// <summary>
    /// The object instance we are accessing
    /// </summary>
    internal QueryNode Instance { get; set; } = instance;

    /// <inheritdoc />
    internal override QueryNodeKind Kind => QueryNodeKind.MemberAccess;

    /// <summary>
    /// The name of the member (property, field, etc.) we are acccessing
    /// </summary>
    internal string MemberName { get; set; } = memberName;

    /// <inheritdoc/>
    internal override T Accept<T>(QueryNodeVisitor<T> visitor) => visitor.Visit(this);
}
