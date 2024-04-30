// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using CommunityToolkit.Datasync.Client.Query.OData;
using CommunityToolkit.Datasync.Common.Test.Models;

namespace CommunityToolkit.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class EdmTypeSupport_Tests
{
    [Fact]
    public void ToODataString_DateOnly()
    {
        DateOnly dateOnly = new(2022, 2, 14);

        EdmTypeSupport.ToODataString(dateOnly).Should().Be("cast(2022-02-14,Edm.Date)");
    }

    [Fact]
    public void ToODataString_TimeOnly()
    {
        TimeOnly timeOnly = new(2, 14, 30);

        EdmTypeSupport.ToODataString(timeOnly).Should().Be("cast(02:14:30,Edm.TimeOfDay)");
    }

    [Fact]
    public void ToODataString_Guid()
    {
        Guid guid = Guid.Parse("430a9943-0c75-439d-b0e6-2ce77948672a");

        EdmTypeSupport.ToODataString(guid).Should().Be("cast(430a9943-0c75-439d-b0e6-2ce77948672a,Edm.Guid)");
    }

    [Fact]
    public void ToODataString_DateTime()
    {
        DateTime dt = DateTime.Parse("2021-03-01T07:30:22.000+00:00");

        EdmTypeSupport.ToODataString(dt).Should().Be("cast(2021-03-01T07:30:22.000Z,Edm.DateTimeOffset)");
    }

    [Fact]
    public void ToODataString_DateTimeOffset()
    {
        DateTimeOffset dt = DateTimeOffset.Parse("2021-03-01T07:30:22.000+00:00");

        EdmTypeSupport.ToODataString(dt).Should().Be("cast(2021-03-01T07:30:22.000Z,Edm.DateTimeOffset)");
    }

    [Fact]
    public void ToODataString_Unknown()
    {
        ClientMovie sut = new() { Id = "1234" };

        EdmTypeSupport.ToODataString(sut).Should().BeNull();
    }

    [Fact]
    public void ToQueryNode_DateOnly()
    {
        DateOnly expected = new(2022, 2, 14);
        QueryNode q = EdmTypeSupport.ToQueryNode("2022-02-14", "Edm.Date");

        q.Kind.Should().Be(QueryNodeKind.Constant);
        (q as ConstantNode).Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToQueryNode_TimeOnly()
    {
        TimeOnly expected = new(2, 14, 30);
        QueryNode q = EdmTypeSupport.ToQueryNode("02:14:30", "Edm.TimeOfDay");

        q.Kind.Should().Be(QueryNodeKind.Constant);
        (q as ConstantNode).Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToQueryNode_Guid()
    {
        Guid expected = Guid.Parse("430a9943-0c75-439d-b0e6-2ce77948672a");
        QueryNode q = EdmTypeSupport.ToQueryNode("430a9943-0c75-439d-b0e6-2ce77948672a", "Edm.Guid");

        q.Kind.Should().Be(QueryNodeKind.Constant);
        (q as ConstantNode).Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToQueryNode_DateTime()
    {
        DateTime expected = DateTime.Parse("2021-03-01T07:30:22.000+00:00");
        QueryNode q = EdmTypeSupport.ToQueryNode("2021-03-01T07:30:22.000Z", "Edm.DateTime");

        q.Kind.Should().Be(QueryNodeKind.Constant);
        (q as ConstantNode).Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToQueryNode_DateTimeOffset()
    {
        DateTimeOffset expected = DateTimeOffset.Parse("2021-03-01T07:30:22.000+00:00");
        QueryNode q = EdmTypeSupport.ToQueryNode("2021-03-01T07:30:22.000Z", "Edm.DateTimeOffset");

        q.Kind.Should().Be(QueryNodeKind.Constant);
        (q as ConstantNode).Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToQueryNode_Unknown()
    {
        Assert.Throws<InvalidOperationException>(() => EdmTypeSupport.ToQueryNode("sut", "Edm.GeoPoint").Should().BeNull());
    }
}
