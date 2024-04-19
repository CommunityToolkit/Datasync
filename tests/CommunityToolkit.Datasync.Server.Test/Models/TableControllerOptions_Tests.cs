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

        sut.DisableClientSideEvaluation.Should().BeFalse();
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

    [Fact]
    public void Ctor_Roundtrips()
    {
        TableControllerOptions sut = new() { DisableClientSideEvaluation = true, EnableSoftDelete = true, MaxTop = 100, PageSize = 50, UnauthorizedStatusCode = 510 };

        sut.DisableClientSideEvaluation.Should().BeTrue();
        sut.EnableSoftDelete.Should().BeTrue();
        sut.MaxTop.Should().Be(100);
        sut.PageSize.Should().Be(50);
        sut.UnauthorizedStatusCode.Should().Be(510);
    }
}
