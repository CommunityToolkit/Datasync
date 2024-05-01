// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query;
using CommunityToolkit.Datasync.Common.Test.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class ODataQuery_Tests
{
    private readonly IReadonlyRemoteDataset<ClientMovie> table = new RemoteDataset<ClientMovie>(new Uri("https://localhost/"));
    private readonly ODataQuery<ClientMovie> query;

    class SelectResult
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    class RequiredSelectResult
    {
        public string Id { get; set; }
        public string Title { get; set; }
        [Required]
        public bool Deleted { get; set; }
    }

    public ODataQuery_Tests()
    {
        this.query = new(this.table, null, null, false);
    }

    [Fact]
    public void Ctor_NullTable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ODataQuery<ClientMovie>(null, null, null, false));
    }

    [Fact]
    public void Ctor_BlankSetup()
    {
        this.query.Dataset.Should().BeSameAs(this.table);
        this.query.Query.Should().NotBeNull();
        this.query.QueryParameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void IncludeDeletedItems_Enabled_ChangesKey()
    {
        this.query.QueryParameters.Add("__includedeleted", "test");
        ODataQuery<ClientMovie> actual = this.query.IncludeDeletedItems() as ODataQuery<ClientMovie>;

        actual.QueryParameters.Should().Contain("__includedeleted", "true");
    }

    [Fact]
    public void IncludeDeletedItems_Disabled_RemovesKey()
    {
        this.query.QueryParameters.Add("__includedeleted", "true");
        ODataQuery<ClientMovie> actual = this.query.IncludeDeletedItems(false) as ODataQuery<ClientMovie>;

        actual.QueryParameters.Should().NotContainKey("__includedeleted");
    }

    [Fact]
    public void IncludeDeletedItems_Disabled_WorksWithEmptyParameters()
    {
        ODataQuery<ClientMovie> actual = this.query.IncludeDeletedItems(false) as ODataQuery<ClientMovie>;

        actual.QueryParameters.Should().NotContainKey("__includedeleted");
    }

    [Fact]
    public void ToODataString_IncludeDeletedItems_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.IncludeDeletedItems(true) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("__includedeleted=true");
    }

    [Fact]
    public void IncludeTotalCount_Enabled_AddsKey()
    {
        ODataQuery<ClientMovie> actual = this.query.IncludeTotalCount(true) as ODataQuery<ClientMovie>;

        actual.RequestTotalCount.Should().BeTrue();
    }

    [Fact]
    public void IncludeTotalCount_Disabled_RemovesKey()
    {
        ODataQuery<ClientMovie> actual = this.query.IncludeTotalCount(false) as ODataQuery<ClientMovie>;

        actual.RequestTotalCount.Should().BeFalse();
    }

    [Fact]
    public void ToODataString_IncludeTotalCount_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.IncludeTotalCount() as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$count=true");
    }

    [Fact]
    public void OrderBy_Null_Throws()
    {
        Expression<Func<ClientMovie, string>> keySelector = null;
        Assert.Throws<ArgumentNullException>(() => this.query.OrderBy(keySelector));
    }

    [Fact]
    public void ToODataString_OrderBy_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.Id) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$orderby=id");
    }

    [Fact]
    public void ToODataString_OrderBy_ThrowsNotSupported()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.Id.ToLower()) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void OrderByDescending_Null_Throws()
    {
        Expression<Func<ClientMovie, string>> keySelector = null;
        Assert.Throws<ArgumentNullException>(() => this.query.OrderByDescending(keySelector));
    }

    [Fact]
    public void ToODataString_OrderByDescending_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderByDescending(m => m.Id) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$orderby=id desc");
    }

    [Fact]
    public void ToODataString_OrderByDescending_ThrowsNotSupported()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderByDescending(m => m.Id.ToLower()) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Select_Null_Throws()
    {
        Expression<Func<ClientMovie, SelectResult>> selector = null;
        Assert.Throws<ArgumentNullException>(() => this.query.Select(selector));
    }

    [Fact]
    public void ToODataString_Select_IsWellFormed()
    {
        ODataQuery<SelectResult> actual = this.query.Select(m => new SelectResult { Id = m.Id, Title = m.Title }) as ODataQuery<SelectResult>;
        string odata = actual.ToODataString();

        odata.Should().Be("$select=id,title");
    }

    [Fact]
    public void ToODataString_Select_RequiredProperty()
    {
        string odata = this.query.Select(m => new RequiredSelectResult { Id = m.Id, Title = m.Title }).ToODataString();

        odata.Should().Be("$select=deleted,id,title");
    }

    [Fact]
    public void ToODataString_Select_NoId_IsWellFormed()
    {
        string odata = this.query.Select(m => new { m.Title, m.ReleaseDate }).ToODataString();

        odata.Should().Be("$select=releaseDate,title");
    }

    [Theory, CombinatorialData]
    public void Skip_Throws_OutOfRange([CombinatorialValues(-10, -1)] int skip)
    {
        Assert.ThrowsAny<ArgumentException>(() => this.query.Skip(skip));
    }

    [Fact]

    public void ToODataString_Skip_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.Skip(5) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$skip=5");
    }

    [Fact]
    public void ToODataString_DoubleSkip()
    {
        ODataQuery<ClientMovie> actual = this.query.Skip(20).Skip(5) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$skip=25");
    }

    [Theory, CombinatorialData]
    public void Take_ThrowsOutOfRange([CombinatorialValues(-10, -1, 0)] int take)
    {
        Assert.ThrowsAny<ArgumentException>(() => this.query.Take(take));
    }

    [Fact]
    public void ToODataString_Take_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.Take(5) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$top=5");
    }

    [Fact]
    public void ToODataString_DoubleTake_Second()
    {
        this.query.Take(20).Take(5).ToODataString().Should().Be("$top=5");
    }

    [Fact]
    public void ToODataString_DoubleTake_First()
    {
        this.query.Take(5).Take(20).ToODataString().Should().Be("$top=5");
    }

    [Fact]
    public void ThenBy_Null_Throws()
    {
        Expression<Func<ClientMovie, string>> keySelector = null;
        Assert.Throws<ArgumentNullException>(() => this.query.ThenBy(keySelector));
    }

    [Fact]
    public void ToODataString_ThenBy_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.ThenBy(m => m.Id) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();
        odata.Should().Be("$orderby=id");
    }

    [Fact]
    public void ToODataString_ThenBy_ThrowsNotSupported()
    {
        ODataQuery<ClientMovie> actual = this.query.ThenBy(m => m.Id.ToLower()) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void ThenByDescending_Null_Throws()
    {
        Expression<Func<ClientMovie, string>> keySelector = null;
        Assert.Throws<ArgumentNullException>(() => this.query.ThenByDescending(keySelector));
    }

    [Fact]
    public void ToODataString_ThenByDescending_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.ThenByDescending(m => m.Id) as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$orderby=id desc");
    }

    [Fact]
    public void ToODataString_ThenByDescending_ThrowsNotSupported()
    {
        ODataQuery<ClientMovie> actual = this.query.ThenByDescending(m => m.Id.ToLower()) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Where_Null_Throws()
    {
        Expression<Func<ClientMovie, bool>> predicate = null;
        Assert.Throws<ArgumentNullException>(() => this.query.Where(predicate));
    }

    [Fact]
    public void ToODataString_Where_IsWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.Where(m => m.Id == "foo") as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$filter=%28id%20eq%20%27foo%27%29");
    }

    [Fact]
    public void ToODataString_DoubleWhere()
    {
        this.query.Where(m => m.Id == "foo").Where(m => m.Title.StartsWith("the")).ToODataString()
            .Should().Be("$filter=%28%28id%20eq%20%27foo%27%29%20and%20startswith%28title%2C%27the%27%29%29");
    }

    [Fact]
    public void ToODataString_Where_ThrowsNotSupported()
    {
        ODataQuery<ClientMovie> actual = this.query.ThenByDescending(m => m.Id.Normalize() == "foo") as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Theory]
    [InlineData(null, "test")]
    [InlineData("test", null)]
    public void WithParameter_Null_Throws(string key, string value)
    {
        Assert.Throws<ArgumentNullException>(() => this.query.WithParameter(key, value));
    }

    [Theory]
    [InlineData("testkey", "")]
    [InlineData("testkey", " ")]
    [InlineData("testkey", "   ")]
    [InlineData("testkey", "\t")]
    [InlineData("", "testvalue")]
    [InlineData(" ", "testvalue")]
    [InlineData("   ", "testvalue")]
    [InlineData("\t", "testvalue")]
    [InlineData("$count", "true")]
    [InlineData("__includedeleted", "true")]
    public void WithParameter_Illegal_Throws(string key, string value)
    {
        Assert.Throws<ArgumentException>(() => this.query.WithParameter(key, value));
    }

    [Fact]
    public void WithParameter_SetsParameter()
    {
        ODataQuery<ClientMovie> actual = this.query.WithParameter("testkey", "testvalue") as ODataQuery<ClientMovie>;

        actual.QueryParameters.Should().Contain("testkey", "testvalue");
    }

    [Fact]
    public void WithParameter_Overwrites()
    {
        ODataQuery<ClientMovie> actual = this.query
            .WithParameter("testkey", "testvalue")
            .WithParameter("testkey", "replacement") as ODataQuery<ClientMovie>;

        actual.QueryParameters.Should().Contain("testkey", "replacement");
    }

    [Fact]
    public void ToODataString_WithParameter_isWellFormed()
    {
        ODataQuery<ClientMovie> actual = this.query.WithParameter("testkey", "testvalue") as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("testkey=testvalue");
    }

    [Fact]
    public void ToODataString_WithParameter_EncodesValue()
    {
        ODataQuery<ClientMovie> actual = this.query.WithParameter("testkey", "test value") as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("testkey=test%20value");
    }

    [Fact]
    public void LinqODataWithSelectConversions()
    {
        DateTimeOffset dto1 = new(1994, 10, 14, 0, 0, 0, TimeSpan.Zero);
        List<string> expectedSegments = [
            "$count=true",
            "$filter=%28updatedAt%20gt%20cast%281994-10-14T00%3A00%3A00.000Z%2CEdm.DateTimeOffset%29%29",
            "$orderby=updatedAt",
            "$select=id,title",
            "$skip=25",
            "__includedeleted=true"
        ];

        ODataQuery<SelectResult> actual = this.query
            .Where(x => x.UpdatedAt > dto1)
            .IncludeDeletedItems()
            .OrderBy(x => x.UpdatedAt)
            .IncludeTotalCount()
            .Skip(25)
            .Select(m => new SelectResult { Id = m.Id, Title = m.Title }) as ODataQuery<SelectResult>;
        string odata = actual.ToODataString();

        odata.Split('&').Should().BeEquivalentTo(expectedSegments);
    }

    [Fact]
    public void Linq_NotSupportedProperties()
    {
        ODataQuery<ClientMovie> actual = this.query.Where(m => m.UpdatedAt.Value.UtcDateTime > new DateTime(2001, 12, 31)) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Linq_NotSupportedMethods()
    {
        ODataQuery<ClientMovie> actual = this.query.Where(m => m.Title.LastIndexOf("er") > 0) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Linq_NotSupportedBinaryOperators()
    {
        ODataQuery<ClientMovie> actual = this.query.Where(m => (m.Year ^ 1024) == 0) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Linq_NotSupportedUnaryOperators()
    {
        ODataQuery<ClientMovie> actual = this.query.Where(m => (5 * (-m.Duration)) > -180) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Linq_NotSupportedDistinctLinqStatement()
    {
        // Never modify the internal query (really!)
        this.query.Query = this.query.Query.Distinct();
        Assert.Throws<NotSupportedException>(() => this.query.ToODataString());
    }

    [Fact]
    public void Linq_NegateNotSupported()
    {
        ODataQuery<ClientMovie> actual = this.query.Where(m => (-m.Year) <= -2000) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Linq_InvalidOrderBy_Lambda()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.Id == "foo" ? "yes" : "no") as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Linq_InvalidOrderBy_Method()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.GetHashCode()) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void Linq_InvalidOrderBy_ToString()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.ReleaseDate.ToString("o")) as ODataQuery<ClientMovie>;
        Assert.Throws<NotSupportedException>(() => actual.ToODataString());
    }

    [Fact]
    public void ToODataString_DontIncludeParameters()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.ReleaseDate).WithParameter("foo", "bar") as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString(false);

        odata.Should().Be("$orderby=releaseDate");
    }

    [Fact]
    public void ToODataString_IncludeParameters()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.ReleaseDate).WithParameter("foo", "bar") as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString(true);

        odata.Should().Be("$orderby=releaseDate&foo=bar");
    }

    [Fact]
    public void ToODataString_DefaultIncludeParameters()
    {
        ODataQuery<ClientMovie> actual = this.query.OrderBy(m => m.ReleaseDate).WithParameter("foo", "bar") as ODataQuery<ClientMovie>;
        string odata = actual.ToODataString();

        odata.Should().Be("$orderby=releaseDate&foo=bar");
    }

    [Fact]
    public void Linq_EndsWith_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc"),
            "$filter=endswith%28stringValue%2C%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_EndsWith_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.Ordinal),
            "$filter=endswith%28stringValue%2C%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_EndsWith_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.InvariantCulture),
            "$filter=endswith%28stringValue%2C%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_EndsWith_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=endswith%28tolower%28stringValue%29%2Ctolower%28%27abc%27%29%29"
        );
    }

    [Fact]
    public void Linq_EndsWith_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.EndsWith("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=endswith%28tolower%28stringValue%29%2Ctolower%28%27abc%27%29%29"
        );
    }

    [Fact]
    public void Linq_Equals_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc"),
            "$filter=%28stringValue%20eq%20%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_Equals_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.Ordinal),
            "$filter=%28stringValue%20eq%20%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_Equals_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.InvariantCulture),
            "$filter=%28stringValue%20eq%20%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_Equals_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=%28tolower%28stringValue%29%20eq%20tolower%28%27abc%27%29%29"
        );
    }

    [Fact]
    public void Linq_Equals_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.Equals("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=%28tolower%28stringValue%29%20eq%20tolower%28%27abc%27%29%29"
        );
    }

    [Fact]
    public void Linq_StartsWith_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc"),
            "$filter=startswith%28stringValue%2C%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_StartsWith_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.Ordinal),
            "$filter=startswith%28stringValue%2C%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_StartsWith_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.InvariantCulture),
            "$filter=startswith%28stringValue%2C%27abc%27%29"
        );
    }

    [Fact]
    public void Linq_StartsWith_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=startswith%28tolower%28stringValue%29%2Ctolower%28%27abc%27%29%29"
        );
    }

    [Fact]
    public void Linq_StartsWith_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringValue.StartsWith("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=startswith%28tolower%28stringValue%29%2Ctolower%28%27abc%27%29%29"
        );
    }

    [Fact]
    public void Linq_Contains()
    {
        string[] ratings = ["A", "B"];
        ExecuteWhereQuery(
            m => ratings.Contains(m.StringValue),
            "$filter=stringValue%20in%20%28%27A%27%2C%27B%27%29"
        );
    }

    [Fact]
    public void Linq_DoesNotContain()
    {
        string[] ratings = ["A", "B"];
        ExecuteWhereQuery(
            m => !ratings.Contains(m.StringValue),
            "$filter=not%28stringValue%20in%20%28%27A%27%2C%27B%27%29%29"
        );
    }

    private static void ExecuteWhereQuery(Expression<Func<ClientKitchenSink, bool>> predicate, string expected)
    {
        IReadonlyRemoteDataset<ClientKitchenSink> ds = new RemoteDataset<ClientKitchenSink>(new Uri("https://localhost/"));
        string odata = new ODataQuery<ClientKitchenSink>(ds, null, null, false).Where(predicate).ToODataString();

        odata.Should().Be(expected);
    }
}
