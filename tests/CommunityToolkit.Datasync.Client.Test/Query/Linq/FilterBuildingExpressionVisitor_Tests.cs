// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Linq.Expressions;
using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Test.Query.Linq;

/// <summary>
/// Corner cases only (they shouldn't happen)
/// </summary>
[ExcludeFromCodeCoverage]
public class FilterBuildingExpressionVisitor_Tests
{
    [Fact]
    public void Visitor_Visit_Null_ReturnsNull()
    {
        FilterBuildingExpressionVisitor sut = new(null);
        sut.Visit(null).Should().BeNull();
    }

    [Fact]
    public void GetStringComparisonFromExpression_InvalidNode_Throws()
    {
        MemberAccessNode node = new(null, "foo");
        FilterBuildingExpressionVisitor sut = new(null);
        sut.FilterExpression.Push(node);
        Action act = () => _ = sut.GetStringComparisonFromExpression(null);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetStringComparisonFromExpression_InvalidConstantNode_Throws()
    {
        ConstantNode node = new("1");
        FilterBuildingExpressionVisitor sut = new(null);
        sut.FilterExpression.Push(node);
        Action act = () => _ = sut.GetStringComparisonFromExpression(null);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void IsEnumExpression_NonConvertNode_ReturnsFalse()
    {
        byte[] arr = [];
        UnaryExpression testValue = Expression.ArrayLength(Expression.Constant(arr));
        FilterBuildingExpressionVisitor.IsEnumExpression(testValue).Should().BeFalse();
    }

    [Fact]
    public void IsImplicitConversion_NullMemberName_ReturnsFalse()
    {
        byte[] arr = [];
        UnaryExpression testValue = Expression.ArrayLength(Expression.Constant(arr));
        FilterBuildingExpressionVisitor sut = new(null) { GetMemberName = (_, _) => null };
        sut.IsConversionImplicit(testValue, typeof(int), typeof(int)).Should().BeFalse();
    }

    [Fact]
    public void VisitUnaryExpression_NullMemberName_Throws()
    {
        FilterBuildingExpressionVisitor sut = new(null) { GetMemberName = (_, _) => null };
        UnaryExpression expr = UnaryExpression.Convert(Expression.Constant(5), typeof(double));
        Action act = () => sut.VisitUnaryExpression(expr);
        act.Should().Throw<NotSupportedException>();
    }

    // TODO: Add test for quoted unary expression.

    [Fact]
    public void VisitUnaryExpression_NotRecognized_Throws()
    {
        byte[] arr = [];
        UnaryExpression testValue = Expression.ArrayLength(Expression.Constant(arr));
        FilterBuildingExpressionVisitor sut = new(null);
        Action act = () => _ = sut.VisitUnaryExpression(testValue);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void VisitUnaryExpression_Quoted_Unwraps()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(ClientKitchenSink), "x");
        MemberExpression member = Expression.Property(parameter, "IntValue");
        ConstantExpression constant = Expression.Constant(5);
        BinaryExpression body = Expression.GreaterThan(member, constant);
        Expression<Func<ClientKitchenSink, bool>> expr = Expression.Lambda<Func<ClientKitchenSink, bool>>(body, parameter);

        FilterBuildingExpressionVisitor sut = new(null) { Visit = (operand) => operand };
        Action act = () => _ = sut.VisitUnaryExpression(Expression.Quote(expr));
        act.Should().NotThrow();
    }
}
