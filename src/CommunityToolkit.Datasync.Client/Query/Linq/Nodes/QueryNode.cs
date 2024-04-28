// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

/// <summary>
/// Enumeration for the different kinds of QueryNode.
/// </summary>
internal enum QueryNodeKind
{
    Constant = 0,
    UnaryOperator,
    BinaryOperator,
    FunctionCall,
    MemberAccess,
    Convert
}

/// <summary>
/// The base class for all types of QueryNode.
/// </summary>
/// <remarks>
/// When the <see cref="QueryTranslator{T}"/> is walking the expression tree, it builds up a tree of <see cref="QueryNode"/> objects
/// to represent expressions.  All the nodes in the tree are derived from this class so that it can be walked by a visitor later on.
/// </remarks>
internal abstract class QueryNode
{
    /// <summary>
    /// Accept a QueryNodeVisitor that walks a tree of type QueryNode.
    /// </summary>
    /// <typeparam name="T">The type that the visitor will return after visiting this token.</typeparam>
    /// <param name="visitor">The visitor.</param>
    internal abstract T Accept<T>(QueryNodeVisitor<T> visitor);

    /// <summary>
    /// The type of the QueryNode
    /// </summary>
    internal abstract QueryNodeKind Kind { get; }

    /// <summary>
    /// Sets the children for this QueryNode.  Note that not all query nodes support
    /// children, so this is not always called.  If it is called when unexpected, a
    /// <see cref="NotSupportedException"/> will be thrown.
    /// </summary>
    /// <param name="children">The list of children to set.</param>
    internal virtual void SetChildren(IList<QueryNode> children)
    {
        throw new NotSupportedException();
    }
}
