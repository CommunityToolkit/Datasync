// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server.Test.Models;

[ExcludeFromCodeCoverage]
public class PagedResult_Tests
{
    [Fact]
    public void PagedResult_DefaultConstructor()
    {
        PagedResult result = new();

        result.Items.Should().NotBeNull().And.BeEmpty();
        result.Count.Should().BeNull();
        result.NextLink.Should().BeNullOrEmpty();
    }

    [Fact]
    public void PagedResult_EmptyConstructor()
    {
        PagedResult result = new([]);

        result.Items.Should().NotBeNull().And.BeEmpty();
        result.Count.Should().BeNull();
        result.NextLink.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Items_Set()
    {
        List<object> items = new([1, 2, 3]);
        PagedResult result = new()
        {
            Items = items
        };

        result.Items.Should().NotBeNull().And.BeEquivalentTo(items);
    }
}
