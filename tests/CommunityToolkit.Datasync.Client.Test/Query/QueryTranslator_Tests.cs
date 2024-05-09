// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client.Test.Query;

// Most tests for this class are done in the Linq tests.  However, there are a few
// corner cases that are not easy to come up with reasonable tests for because they
// *shouldn't* happen.
[ExcludeFromCodeCoverage]
public class QueryTranslator_Tests
{
    private readonly RemoteDataset<ClientMovie> table;
    private readonly IODataQuery<ClientMovie> query;
    private readonly QueryTranslator<ClientMovie> translator;

    class NamedSelectResult
    {
        public string Id { get; set; }
        [JsonPropertyName("title")]
        public string JsonTitle { get; set; }
    }

    public QueryTranslator_Tests()
    {
        this.table = new(new Uri("https://localhost/"));
        this.query = this.table.AsQueryable();
        this.translator = new QueryTranslator<ClientMovie>(this.query);
    }
    [Fact]
    public void AddFilter_Null_Throws()
    {
        Action act = () => this.translator.AddFilter(null);
        act.Should().Throw<NotSupportedException>();
    }

    [Theory, CombinatorialData]
    public void AddOrdering_Null_Throws(bool ascending, bool prepend)
    {
        Action act = () => this.translator.AddOrdering(null, ascending, prepend);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddProjection_Null_Throws()
    {
        Action act = () => this.translator.AddProjection(null);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetCountArgument_Null_Throws()
    {
        Action act = () => this.translator.GetCountArgument(null);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void StripQuote_ReturnsExpression_WhenNotQuoted()
    {
        UnaryExpression typeAsExpression = Expression.TypeAs(Expression.Constant(34, typeof(int)), typeof(int?));
        Expression result = this.translator.StripQuote(typeAsExpression);
        result.Should().BeSameAs(typeAsExpression);
    }

    [Fact]
    public void FilterBuildingExpressionVisitor_CompileNull_ReturnsNull()
    {
        QueryNode result = FilterBuildingExpressionVisitor.Compile(null);
        result.Should().BeNull();
    }

    [Fact]
    public void FilterBuildingExpressionVisitor_JsonPropertyName()
    {
        MemberInfo idProperty = typeof(NamedSelectResult).GetProperty("Id");
        FilterBuildingExpressionVisitor.JsonPropertyName(idProperty).Should().Be("id");

        MemberInfo titleProperty = typeof(NamedSelectResult).GetProperty("JsonTitle");
        FilterBuildingExpressionVisitor.JsonPropertyName(titleProperty).Should().Be("title");
    }

    [Fact]
    public void FilterBuildingExpressionVisitor_GetMemberName_Convert()
    {
        ClientMovie clientMovie = new();
        Expression convertExpression = Expression.Convert(Expression.Constant(clientMovie, typeof(ClientMovie)), typeof(IMovie));
        Expression expression = Expression.Property(convertExpression, "Rating");
        FilterBuildingExpressionVisitor.GetMemberName(expression).Should().Be("rating");
    }

    [Fact]
    public void FilterBuildingExpressionVisitor_GetStringComparisonFromQueryNode_NotConstant()
    {
        UnaryOperatorNode node = new(UnaryOperatorKind.Not, new ConstantNode(1));
        Action act = () => FilterBuildingExpressionVisitor.GetStringComparisonFromQueryNode(node);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void FilterBuildingExpressionVisitor_GetStringComparisonFromQueryNode_NotStringComparison()
    {
        ConstantNode node = new("foo");
        Action act = () => FilterBuildingExpressionVisitor.GetStringComparisonFromQueryNode(node);
        act.Should().Throw<NotSupportedException>();
    }
}
