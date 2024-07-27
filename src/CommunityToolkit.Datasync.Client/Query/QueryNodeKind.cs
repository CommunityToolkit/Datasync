// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// The enumeration of all the different kinds of query node.
/// </summary>
internal enum QueryNodeKind
{
    BinaryOperator,
    Constant,
    Convert,
    FunctionCall,
    MemberAccess,
    UnaryOperator
}
