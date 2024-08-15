// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class ExecutableOperation_Tests
{
    [Fact]
    public async Task CreateAsync_InvalidKind()
    {
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = (OperationKind)9999,
            State = OperationState.Pending,
            EntityType = string.Empty,
            ItemId = string.Empty,
            EntityVersion = string.Empty,
            Item = string.Empty,
            Sequence = 0,
            Version = 0
        };

        Func<Task> act = async () => _ = await ExecutableOperation.CreateAsync(op);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Theory]
    [InlineData(null, "https://test.zumo.com/tables/movies", "https://test.zumo.com/tables/movies/")]
    [InlineData(null, "https://test.zumo.com/tables/movies/", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com", "/tables/movies", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com", "/tables/movies/", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com/", "/tables/movies", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com/", "/tables/movies/", "https://test.zumo.com/tables/movies/")]
    [InlineData("https://test.zumo.com/tables", "movies", "https://test.zumo.com/movies/")]
    [InlineData("https://test.zumo.com/tables", "movies/", "https://test.zumo.com/movies/")]
    [InlineData("https://test.zumo.com/tables", "/api/movies", "https://test.zumo.com/api/movies/")]
    [InlineData("https://test.zumo.com/tables", "/api/movies/", "https://test.zumo.com/api/movies/")]
    public void MakeAbsoluteUri_Works(string ba, string bb, string expected)
    {
        Uri arg1 = string.IsNullOrEmpty(ba) ? null : new Uri(ba, UriKind.Absolute);
        Uri arg2 = bb.StartsWith("http") ? new Uri(bb, UriKind.Absolute) : new Uri(bb, UriKind.Relative);
        Uri actual = ExecutableOperation.MakeAbsoluteUri(arg1, arg2);

        actual.ToString().Should().Be(expected);
    }

    [Fact]
    public void MakeAbsoluteUri_BaseAddressRelative()
    {
        Uri arg1 = new("tables/movies", UriKind.Relative);
        Uri arg2 = new("tables/movies", UriKind.Relative);

        Action act = () => ExecutableOperation.MakeAbsoluteUri(arg1, arg2);
        act.Should().Throw<UriFormatException>();
    }
}
