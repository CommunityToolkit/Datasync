// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Test.Query.Linq;

/// <summary>
/// Corner cases only
/// </summary>
[ExcludeFromCodeCoverage]
public class ExpressionExtension_Tests
{
    [Fact]
    public void BinaryOperatorKind_Invalid()
    {
        ExpressionType sut = ExpressionType.Quote;
        Action act = () => _ = sut.ToBinaryOperatorKind();
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void IsValidLambdaExpression_Null()
    {
        MethodCallExpression sut = null;
        ExpressionExtensions.IsValidLambdaExpression(sut, out LambdaExpression lambda).Should().BeFalse();
        lambda.Should().BeNull();
    }

    [Fact]
    public void IsValidLambdaExpression_SingleArg()
    {
        string x = string.Empty;
        MethodInfo toStringMethod = typeof(object).GetTypeInfo().GetDeclaredMethod("ToString");
        MethodCallExpression sut = Expression.Call(Expression.Constant(x), toStringMethod);
        ExpressionExtensions.IsValidLambdaExpression(sut, out LambdaExpression lambda).Should().BeFalse();
        lambda.Should().BeNull();
    }

    [Fact]
    public void IsValidLambdaExpression_NotLambda()
    {
        string x = string.Empty;
        MethodInfo endsWithMethod = typeof(string).GetRuntimeMethod("EndsWith", [typeof(string), typeof(StringComparison)]);
        MethodCallExpression sut = Expression.Call(Expression.Constant(x), endsWithMethod, [Expression.Constant("foo"), Expression.Constant(StringComparison.Ordinal)]);
        ExpressionExtensions.IsValidLambdaExpression(sut, out LambdaExpression lambda).Should().BeFalse();
        lambda.Should().BeNull();
    }

    /// <summary>
    /// A quoted expression is unpacked.  A non-quoted expression is returned as-is.
    /// However, I've been unable to generate a test case where a non-quoted expression
    /// is actually returned.
    /// </summary>
    [Fact]
    public void StripQuote_NonQuoted()
    {
        Expression sut = Expression.Constant("1");
        sut.StripQuote().Should().BeSameAs(sut);
    }
}
