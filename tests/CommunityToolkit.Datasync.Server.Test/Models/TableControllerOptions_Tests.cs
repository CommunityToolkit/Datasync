// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;

namespace CommunityToolkit.Datasync.Server.Test.Models;

[ExcludeFromCodeCoverage]
public class TableControllerOptions_Tests
{
    [Fact]
    public void Ctor_DefaultsDontChange()
    {
        TableControllerOptions sut = new();

        sut.EnableSoftDelete.Should().BeFalse();
        sut.MaxTop.Should().Be(128000);
        sut.PageSize.Should().Be(100);
        sut.UnauthorizedStatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Theory]
    [InlineData(0, 10000)]
    [InlineData(-1, 10000)]
    [InlineData(128001, 10000)]
    [InlineData(100, -1)]
    [InlineData(100, 0)]
    [InlineData(100, 128001)]
    public void Ctor_NoNegativeNumbers(int pageSize, int maxTop)
    {
        Action act = () => _ = new TableControllerOptions { PageSize = pageSize, MaxTop = maxTop };
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(200)]
    [InlineData(399)]
    [InlineData(500)]
    [InlineData(510)]
    public void Ctor_InvalidUnauthorizedStatusCode_Throws(int statusCode)
    {
        Action act = () => _ = new TableControllerOptions { UnauthorizedStatusCode = statusCode };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(499)]
    public void Ctor_ValidUnauthorizedStatusCode_Roundtrips(int statusCode)
    {
        TableControllerOptions sut = new() { UnauthorizedStatusCode = statusCode };
        sut.UnauthorizedStatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void Ctor_Roundtrips()
    {
        TableControllerOptions sut = new() { EnableSoftDelete = true, MaxTop = 100, PageSize = 50, UnauthorizedStatusCode = 403 };

        sut.EnableSoftDelete.Should().BeTrue();
        sut.MaxTop.Should().Be(100);
        sut.PageSize.Should().Be(50);
        sut.UnauthorizedStatusCode.Should().Be(403);
    }
}
