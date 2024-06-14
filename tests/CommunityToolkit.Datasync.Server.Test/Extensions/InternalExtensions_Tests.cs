// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Text.Json;
using System.Web;

namespace CommunityToolkit.Datasync.Server.Test.Extensions;

[ExcludeFromCodeCoverage]
public class InternalExtensions_Tests
{
    #region Test Artifacts
    private readonly TableData testEntity = new()
    {
        Id = "",
        Deleted = false,
        UpdatedAt = DateTimeOffset.Parse("2023-11-13T12:53:13.123Z"),
        Version = [0x01, 0x00, 0x42, 0x22, 0x47, 0x8F]
    };

    private const string matchingETag = "\"AQBCIkeP\"";
    private const string nonMatchingETag = "\"Foo\"";

    private const string earlierTestDate = "Mon, 13 Nov 2023 11:57:00 GMT";
    private const string laterTestDate = "Mon, 13 Nov 2023 14:30:00 GMT";

    class TestObject
    {
        public TestObject Arg { get; set; }
    }
    #endregion

    #region EntityIsInView<TEntity>(IAccessControlProvider<TEntity>, TEntity)
    [Fact]
    public void EntityIsInView_NullDataView_Works()
    {
        TableData entity = new() { Id = "1" };
        IAccessControlProvider<TableData> sut = Substitute.For<IAccessControlProvider<TableData>>();
        Expression<Func<TableData, bool>> expr = null;
        sut.GetDataView().Returns(expr);

        sut.EntityIsInView(entity).Should().BeTrue();
    }

    [Theory]
    [InlineData("inview", true)]
    [InlineData("notinview", false)]
    public void EntityIsInView_Works(string id, bool expected)
    {
        TableData entity = new() { Id = id };
        IAccessControlProvider<TableData> sut = Substitute.For<IAccessControlProvider<TableData>>();
        Expression<Func<TableData, bool>> expr = model => model.Id == "inview";
        sut.GetDataView().Returns(expr);

        sut.EntityIsInView(entity).Should().Be(expected);
    }
    #endregion

    #region IsAfter(DateTimeOffset, DateTimeOffset?)
    [Theory]
    [InlineData(null, true)]
    [InlineData(earlierTestDate, true)]
    [InlineData(laterTestDate, false)]
    public void DateTimeOffset_IsAfter_NullComparison(string dateString, bool expected)
    {
        DateTimeOffset sut = DateTimeOffset.Parse("2023-11-13T12:53:13.123Z");
        DateTimeOffset? dto = dateString == null ? null : DateTimeOffset.Parse(dateString);
        sut.IsAfter(dto).Should().Be(expected);
    }
    #endregion

    #region IsBefore(DateTimeOffset, DateTimeOffset?)
    [Theory]
    [InlineData(null, true)]
    [InlineData(earlierTestDate, false)]
    [InlineData(laterTestDate, true)]
    public void DateTimeOffset_IsBefore_NullComparison(string dateString, bool expected)
    {
        DateTimeOffset sut = DateTimeOffset.Parse("2023-11-13T12:53:13.123Z");
        DateTimeOffset? dto = dateString == null ? null : DateTimeOffset.Parse(dateString);
        sut.IsBefore(dto).Should().Be(expected);
    }
    #endregion

    #region Matches(EntityTagHeaderValue, byte[])
    [Theory]
    [InlineData(false, null, true, true)]
    [InlineData(false, matchingETag, true, true)]
    [InlineData(false, nonMatchingETag, true, false)]
    [InlineData(true, matchingETag, true, false)]
    [InlineData(true, nonMatchingETag, true, false)]
    [InlineData(false, null, false, false)]
    [InlineData(false, matchingETag, false, false)]
    [InlineData(false, nonMatchingETag, false, false)]
    [InlineData(true, matchingETag, false, false)]
    [InlineData(true, nonMatchingETag, false, false)]
    public void EntityTagHeaderValue_Matches_Working(bool isWeak, string tagValue, bool useRealVersion, bool expected)
    {
        EntityTagHeaderValue sut = tagValue == null ? EntityTagHeaderValue.Any : new(tagValue, isWeak);
        byte[] version = useRealVersion ? this.testEntity.Version : [];
        sut.Matches(version).Should().Be(expected);
    }
    #endregion

    #region ParseConditionalRequest<TEntity>(HttpRequest, TEntity, out byte[])
    [Theory]
    [InlineData("GET", null, null, false)]
    [InlineData("POST", null, null, false)]
    [InlineData("Get", "If-Match", matchingETag, true)]
    [InlineData("get", "If-None-Match", nonMatchingETag, false)]
    [InlineData("geT", "If-Modified-Since", earlierTestDate, false)]
    [InlineData("GEt", "If-Unmodified-Since", laterTestDate, false)]
    [InlineData("Post", "If-Match", matchingETag, true)]
    [InlineData("post", "If-None-Match", nonMatchingETag, false)]
    [InlineData("poST", "If-Modified-Since", earlierTestDate, false)]
    [InlineData("POst", "If-Unmodified-Since", laterTestDate, false)]
    public void ParseConditionalRequest_Success(string method, string headerName, string headerValue, bool expectedVersion)
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Method = method;
        if (headerName != null && headerValue != null)
        {
            context.Request.Headers[headerName] = headerValue;
        }

        context.Request.ParseConditionalRequest<TableData>(this.testEntity, out byte[] version);

        if (expectedVersion)
        {
            version.Should().BeEquivalentTo(this.testEntity.Version);
        }
        else
        {
            version.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("Get", "If-Match", nonMatchingETag, 412)]
    [InlineData("get", "If-None-Match", matchingETag, 304)]
    [InlineData("get", "If-Modified-Since", laterTestDate, 304)]
    [InlineData("geT", "If-Unmodified-Since", earlierTestDate, 412)]
    [InlineData("Post", "If-Match", nonMatchingETag, 412)]
    [InlineData("post", "If-None-Match", matchingETag, 412)]
    [InlineData("POST", "If-Modified-Since", laterTestDate, 412)]
    [InlineData("PoSt", "If-Unmodified-Since", earlierTestDate, 412)]
    public void ParseConditionalRequest_Failure(string method, string headerName, string headerValue, int expectedStatusCode)
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Method = method;
        if (headerName != null && headerValue != null)
        {
            context.Request.Headers[headerName] = headerValue;
        }

        Action act = () => context.Request.ParseConditionalRequest<TableData>(this.testEntity, out byte[] version);

        if (expectedStatusCode == 412)
        {
            act.Should().Throw<HttpException>().WithStatusCode(expectedStatusCode).And.WithPayload(this.testEntity);
        }
        else
        {
            act.Should().Throw<HttpException>().WithStatusCode(expectedStatusCode);
        }
    }
    #endregion

    #region SetConditionalHeaders(IHeaderDictionary, ITableData)
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void AddHeadersFromEntity_RemovesEntityHeaders(bool includeETag, bool includeLastModified, bool setUpdatedAtToNull)
    {
        HeaderDictionary headers = [];
        if (includeETag)
        {
            headers.Add("ETag", "\"foo\"");
        }

        if (includeLastModified)
        {
            headers.Add("Last-Modified", "Wed, 21 Oct 2015 07:28:00 GMT");
        }

        ITableData entity = new TableData() { Version = [], UpdatedAt = setUpdatedAtToNull ? null : default(DateTimeOffset) };

        headers.SetConditionalHeaders(entity);

        headers.Should().NotContainKey("ETag");
        headers.Should().NotContainKey("Last-Modified");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddHeadersFromEntity_AddsETagHeader(bool includeHeader)
    {
        HeaderDictionary headers = [];
        if (includeHeader)
        {
            headers.Add("ETag", "\"foo\"");
        }

        ITableData entity = new TableData() { Version = [0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68], UpdatedAt = null };

        headers.SetConditionalHeaders(entity);

        headers.Should().ContainKey("ETag").WhoseValue.Should().ContainSingle(v => v == "\"YWJjZGVmZ2g=\"");
        headers.Should().NotContainKey("LastModified");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddHeadersFromEntity_AddLastModifiedHeader(bool includeHeader)
    {
        HeaderDictionary headers = [];
        if (includeHeader)
        {
            headers.Add("Last-Modified", "Wed, 21 Oct 2015 07:28:00 GMT");
        }

        ITableData entity = new TableData() { Version = [], UpdatedAt = DateTimeOffset.Parse("2023-11-13T13:30:05.1234Z") };

        headers.SetConditionalHeaders(entity);

        headers.Should().NotContainKey("ETag");
        headers.Should().ContainKey("Last-Modified").WhoseValue.Should().ContainSingle(v => v == "Mon, 13 Nov 2023 13:30:05 GMT");
    }

    #endregion

    #region ShouldIncludeDeletedEntities(HttpRequest)
    [Theory]
    [InlineData("__includedeleted=false", false)]
    [InlineData("", false)]
    [InlineData("$filter=deleted eq false", false)]
    [InlineData("__includedeleted=true", true)]
    public void ShouldIncludeDeletedItems_Works(string queryString, bool expected)
    {
        DefaultHttpContext context = new();

        NameValueCollection nvc = HttpUtility.ParseQueryString(queryString);
        Dictionary<string, StringValues> dict = nvc.AllKeys.ToDictionary(k => k, k => new StringValues(nvc[k]));
        context.Request.Query = new QueryCollection(dict);

        context.Request.ShouldIncludeDeletedEntities().Should().Be(expected);
    }
    #endregion

    #region ToByteArray(EntityTagHeaderValue)
    [Fact]
    public void EntityTagHeaderValue_ToByteArray_Wildcard_ReturnsEmptyArray()
    {
        EntityTagHeaderValue sut = new("*");
        byte[] actual = sut.ToByteArray();
        actual.Should().BeEmpty();
    }

    [Fact]
    public void EntityTagHeaderValue_ToByteArray_Works()
    {
        EntityTagHeaderValue sut = new(matchingETag);
        sut.ToByteArray().Should().BeEquivalentTo(this.testEntity.Version);
    }
    #endregion

    #region ToJsonString(object, JsonSerializerOptions?)
    [Fact]
    public void ToJsonString_Null_Works()
    {
        object sut = null;
        string actual = sut.ToJsonString();
        actual.Should().Be("null");
    }

    [Fact]
    public void ToJsonString_ITableData_Works()
    {
        TableData sut = new()
        {
            Id = "0f89592b-6e41-4fe1-abf2-ceee073a6d53",
            Deleted = false,
            Version = [0x61, 0x62, 0x63, 0x64],
            UpdatedAt = new DateTimeOffset(2022, 10, 21, 7, 28, 0, TimeSpan.Zero)
        };
        const string expected = "{\"$id\":\"1\",\"Id\":\"0f89592b-6e41-4fe1-abf2-ceee073a6d53\",\"Deleted\":false,\"UpdatedAt\":\"2022-10-21T07:28:00.000Z\",\"Version\":\"YWJjZA==\"}";

        sut.ToJsonString().Should().Be(expected);
    }

    [Fact]
    public void ToJsonString_Catches_UnserializableObjects()
    {
        JsonSerializerOptions options = InternalExtensions.GetSerializerOptions();
        options.ReferenceHandler = null;

        // Circular references *should* cause an exception.
        TestObject sut = new(); sut.Arg = sut;

        string actual = sut.ToJsonString(options);
        actual.Should().Be("unserializable object");
    }
    #endregion
}
