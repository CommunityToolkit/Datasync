// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// Enumerator of the supported binary operators.
/// </summary>
internal enum BinaryOperatorKind
{
    Or,
    And,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo
}

/// <summary>
/// A set of conversion extensions for the <see cref="BinaryOperatorKind"/> enum.
/// </summary>
internal static class BinaryOperatorKindExtensions
{
    /// <summary>
    /// Convert an <see cref="ExpressionType"/> into a <see cref="BinaryOperatorKind"/>
    /// </summary>
    /// <param name="type">The <see cref="ExpressionType"/></param>
    /// <returns>The equivalent <see cref="BinaryOperatorKind"/></returns>
    internal static BinaryOperatorKind ToBinaryOperatorKind(this ExpressionType type) => type switch
    {
        ExpressionType.Add => BinaryOperatorKind.Add,
        ExpressionType.AndAlso => BinaryOperatorKind.And,
        ExpressionType.Divide => BinaryOperatorKind.Divide,
        ExpressionType.Equal => BinaryOperatorKind.Equal,
        ExpressionType.GreaterThan => BinaryOperatorKind.GreaterThan,
        ExpressionType.GreaterThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual,
        ExpressionType.LessThan => BinaryOperatorKind.LessThan,
        ExpressionType.LessThanOrEqual => BinaryOperatorKind.LessThanOrEqual,
        ExpressionType.Modulo => BinaryOperatorKind.Modulo,
        ExpressionType.Multiply => BinaryOperatorKind.Multiply,
        ExpressionType.NotEqual => BinaryOperatorKind.NotEqual,
        ExpressionType.OrElse => BinaryOperatorKind.Or,
        ExpressionType.Subtract => BinaryOperatorKind.Subtract,
        _ => throw new NotSupportedException($"The operator '{type}' is not supported in the 'Where' query expression")
    };

    /// <summary>
    /// Converts the <see cref="BinaryOperatorKind"/> to an OData operator.
    /// </summary>
    internal static string ToODataString(this BinaryOperatorKind kind) => kind switch
    {
        BinaryOperatorKind.Or => "or",
        BinaryOperatorKind.And => "and",
        BinaryOperatorKind.Equal => "eq",
        BinaryOperatorKind.NotEqual => "ne",
        BinaryOperatorKind.GreaterThan => "gt",
        BinaryOperatorKind.GreaterThanOrEqual => "ge",
        BinaryOperatorKind.LessThan => "lt",
        BinaryOperatorKind.LessThanOrEqual => "le",
        BinaryOperatorKind.Add => "add",
        BinaryOperatorKind.Subtract => "sub",
        BinaryOperatorKind.Multiply => "mul",
        BinaryOperatorKind.Divide => "div",
        BinaryOperatorKind.Modulo => "mod",
        _ => throw new NotSupportedException($"'{kind}' is not supported in a 'Where' table query expression.")
    };
}
