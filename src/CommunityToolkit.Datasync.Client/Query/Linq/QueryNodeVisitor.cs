// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

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
    public abstract T Visit(BinaryOperatorNode node);

    /// <summary>
    /// Visit a <see cref="ConstantNode"/>
    /// </summary>
    public abstract T Visit(ConstantNode node);

    /// <summary>
    /// Visit a <see cref="ConvertNode"/>
    /// </summary>
    public abstract T Visit(ConvertNode node);

    /// <summary>
    /// Visit a <see cref="FunctionCallNode"/>
    /// </summary>
    public abstract T Visit(FunctionCallNode node);

    /// <summary>
    /// Visit a <see cref="MemberAccessNode"/>
    /// </summary>
    public abstract T Visit(MemberAccessNode node);

    /// <summary>
    /// Visit a <see cref="UnaryOperatorNode"/>
    /// </summary>
    public abstract T Visit(UnaryOperatorNode node);
}
