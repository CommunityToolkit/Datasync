// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Service;
using CommunityToolkit.Datasync.TestCommon.Databases;
using NSubstitute;
using System.Linq.Expressions;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class IDatasyncQueryable_Tests
{
    #region Boolean Comparisons
    [Fact]
    public void Linq_Boolean_IsFalse()
    {
        ExecuteWhereQuery(
            m => !m.BooleanValue,
            "$filter=not(booleanValue)");

        ExecuteWhereQuery(
            m => m.BooleanValue == false,
            "$filter=(booleanValue eq false)");
    }

    [Fact]
    public void Linq_Boolean_IsTrue()
    {
        ExecuteWhereQuery(
            m => m.BooleanValue,
            "$filter=booleanValue");

        ExecuteWhereQuery(
            m => m.BooleanValue == true,
            "$filter=(booleanValue eq true)");
    }

    [Fact]
    public void Linq_Boolean_IsNotFalse()
    {
        ExecuteWhereQuery(
            m => m.BooleanValue != false,
            "$filter=(booleanValue ne false)");
    }

    [Fact]
    public void Linq_Boolean_IsNotTrue()
    {
        ExecuteWhereQuery(
            m => m.BooleanValue != true,
            "$filter=(booleanValue ne true)");
    }
    #endregion

    #region String Comparisons
    [Fact]
    public void Linq_EndsWith_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc"),
            "$filter=endswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_EndsWith_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.Ordinal),
            "$filter=endswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_EndsWith_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.InvariantCulture),
            "$filter=endswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_EndsWith_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=endswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_EndsWith_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=endswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Equals_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc"),
            "$filter=(stringValue eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Equals_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.Ordinal),
            "$filter=(stringValue eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Equals_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.InvariantCulture),
            "$filter=(stringValue eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Equals_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=(tolower(stringValue) eq tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Equals_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=(tolower(stringValue) eq tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_StartsWith_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc"),
            "$filter=startswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_StartsWith_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.Ordinal),
            "$filter=startswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_StartsWith_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.InvariantCulture),
            "$filter=startswith(stringValue,'abc')"
        );
    }

    [Fact]
    public void Linq_StartsWith_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=startswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_StartsWith_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=startswith(tolower(stringValue),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Contains()
    {
        string[] ratings = ["A", "B"];
        ExecuteWhereQuery(
            m => ratings.Contains(m.StringValue),
            "$filter=stringValue in ('A','B')"
        );
    }
    #endregion

    private static void ExecuteWhereQuery(Expression<Func<ClientKitchenSink, bool>> predicate, string expected)
    {
        JsonSerializerOptions serializerOptions = DatasyncSerializer.JsonSerializerOptions;
        DatasyncServiceClient<ClientKitchenSink> client = new(new Uri("http://localhost/tables/kitchensink"), new HttpClient(), serializerOptions);
        DatasyncQueryable<ClientKitchenSink> query = new(client, Array.Empty<ClientKitchenSink>().AsQueryable(), new Dictionary<string, string>(), false);
        string actual = Uri.UnescapeDataString(query.Where(predicate).ToODataQueryString());
        Assert.Equal(expected, actual);
    }
}
