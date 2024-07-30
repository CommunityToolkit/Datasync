// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Service;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Query.Linq;

[ExcludeFromCodeCoverage]
public class QueryTranslator_Tests
{
    private readonly IDatasyncQueryable<ClientKitchenSink> query;
    private readonly QueryTranslator<ClientKitchenSink> queryTranslator;

    public QueryTranslator_Tests()
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        this.query = client.AsQueryable();
        this.queryTranslator = new(this.query);
    }

    [Fact]
    public void AddByMethodName_UnknownMethodName_Throws()
    {      
        Action act = () => this.queryTranslator.AddByMethodName(null, "NotFound");
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddFilter_NotValidLambda_Throws()
    {
        string x = string.Empty;
        MethodInfo toStringMethod = typeof(object).GetTypeInfo().GetDeclaredMethod("ToString");
        MethodCallExpression expr = Expression.Call(Expression.Constant(x), toStringMethod);
        Action act = () => this.queryTranslator.AddFilter(expr);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddOrdering_Null_Throws()
    {
        Action act = () => this.queryTranslator.AddOrdering(null, false, false);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddOrdering_NotValidLambda_Throws()
    {
        string x = string.Empty;
        MethodInfo toStringMethod = typeof(object).GetTypeInfo().GetDeclaredMethod("ToString");
        MethodCallExpression expr = Expression.Call(Expression.Constant(x), toStringMethod);
        Action act = () => this.queryTranslator.AddOrdering(expr, false, false);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddOrdering_NotMemberAccess_Throws()
    {
        string x = string.Empty;
        MethodInfo startsWithMethod = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)]);
        MethodCallExpression expr = Expression.Call(Expression.Constant(x), startsWithMethod, Expression.Constant(x), Expression.Constant(StringComparison.Ordinal));
        Action act = () => this.queryTranslator.AddOrdering(expr, false, false);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddOrdering_NullMemberAccess_Throws()
    {
        IDatasyncQueryable<ClientKitchenSink> myQuery = this.query.OrderBy(x => x.Id);
        QueryTranslator<ClientKitchenSink> qt = new(myQuery) { GetTableMemberName = (_, _) => null };
        Action act = () => _ = qt.Translate();
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddProjection_NotValidLambda_Throws()
    {
        string x = string.Empty;
        MethodInfo toStringMethod = typeof(object).GetTypeInfo().GetDeclaredMethod("ToString");
        MethodCallExpression expr = Expression.Call(Expression.Constant(x), toStringMethod);
        Action act = () => this.queryTranslator.AddProjection(expr);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetCountArgument_NullExpression_Throws()
    {
        Action act = () => QueryTranslator<ClientKitchenSink>.GetCountArgument(null);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetCountArgument_NotCount_Throws()
    {
        string x = string.Empty;
        MethodInfo toStringMethod = typeof(object).GetTypeInfo().GetDeclaredMethod("ToString");
        MethodCallExpression expr = Expression.Call(Expression.Constant(x), toStringMethod);
        Action act = () => QueryTranslator<ClientKitchenSink>.GetCountArgument(expr);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetCountArgument_NotInt_Throws()
    {
        string x = string.Empty;
        MethodInfo startsWithMethod = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)]);
        MethodCallExpression expr = Expression.Call(Expression.Constant(x), startsWithMethod, Expression.Constant(x), Expression.Constant(StringComparison.Ordinal));
        Action act = () => QueryTranslator<ClientKitchenSink>.GetCountArgument(expr);
        act.Should().Throw<NotSupportedException>();
    }
}
