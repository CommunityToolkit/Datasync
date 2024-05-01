// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// Visitor interface for walking the <see cref="QueryNode"/> tree.
/// </summary>
/// <typeparam name="T">Type produced by the visitor.</typeparam>
internal abstract class QueryNodeVisitor<T>
{
    /// <summary>
    /// Visit a <see cref="BinaryOperatorNode"/>
    /// </summary>
    internal abstract T Visit(BinaryOperatorNode node);

    /// <summary>
    /// Visit a <see cref="ConstantNode"/>
    /// </summary>
    internal abstract T Visit(ConstantNode node);

    /// <summary>
    /// Visit a <see cref="ConvertNode"/>
    /// </summary>
    internal abstract T Visit(ConvertNode node);

    /// <summary>
    /// Visit a <see cref="FunctionCallNode"/>
    /// </summary>
    internal abstract T Visit(FunctionCallNode node);

    /// <summary>
    /// Visit a <see cref="MemberAccessNode"/>
    /// </summary>
    internal abstract T Visit(MemberAccessNode node);

    /// <summary>
    /// Visit a <see cref="UnaryOperatorNode"/>
    /// </summary>
    internal abstract T Visit(UnaryOperatorNode node);
}