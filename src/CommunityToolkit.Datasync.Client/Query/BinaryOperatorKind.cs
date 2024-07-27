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
}
