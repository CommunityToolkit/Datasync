// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class DefaultDeltaTokenStore_Tests : BaseTest
{
    private readonly TestDbContext context = CreateContext();

    IDeltaTokenStore DeltaTokenStore { get => this.context.DeltaTokenStore; }

    [Fact]
    public async Task GetDeltaTokenAsync_ReturnsMinValueWhenMissing()
    {
        DateTimeOffset actual = await DeltaTokenStore.GetDeltaTokenAsync("abc");
        actual.ToUnixTimeMilliseconds().Should().Be(0);
    }

    [Fact]
    public async Task GetDeltaTokenAsync_ReturnsValueWhenPresent()
    {
        DateTimeOffset expected = DateTimeOffset.UtcNow;
        this.context.DatasyncDeltaTokens.Add(new DatasyncDeltaToken() { Id = "abc", Value = expected.ToUnixTimeMilliseconds() });
        this.context.SaveChanges();
        DateTimeOffset actual = await DeltaTokenStore.GetDeltaTokenAsync("abc");
        actual.ToUnixTimeMilliseconds().Should().Be(expected.ToUnixTimeMilliseconds());
    }

    [Theory]
    [MemberData(nameof(ClientTestData.InvalidIds), MemberType = typeof(ClientTestData))]
    public async Task GetDeltaTokenAsync_Throws_InvalidId(string queryId)
    {
        Func<Task> act = async () => _ = await DeltaTokenStore.GetDeltaTokenAsync(queryId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ResetDeltaTokenAsync_Noop_WhenMissing()
    {
        Func<Task> act = async () =>
        {
            await DeltaTokenStore.ResetDeltaTokenAsync("abc");
            this.context.SaveChanges();
        };
        await act.Should().NotThrowAsync();
        this.context.DatasyncDeltaTokens.ToList().Should().BeEmpty();
    }

    [Fact]
    public async Task ResetDeltaTokenAsync_Removes_WhenPresent()
    {
        DateTimeOffset expected = DateTimeOffset.UtcNow;
        this.context.DatasyncDeltaTokens.Add(new DatasyncDeltaToken() { Id = "abc", Value = expected.ToUnixTimeMilliseconds() });
        this.context.SaveChanges();
        Func<Task> act = async () =>
        {
            await DeltaTokenStore.ResetDeltaTokenAsync("abc");
            this.context.SaveChanges();
        };
        await act.Should().NotThrowAsync();
        this.context.DatasyncDeltaTokens.ToList().Should().BeEmpty();
    }

    [Fact]
    public async Task SetDeltaTokenAsync_Adds_WhenMissing()
    {
        DateTimeOffset value = DateTimeOffset.UtcNow;
        DatasyncDeltaToken expected = new() { Id = "abc", Value = value.ToUnixTimeMilliseconds() };
        Func<Task> act = async () =>
        {
            await DeltaTokenStore.SetDeltaTokenAsync("abc", value);
            this.context.SaveChanges();
        };
        await act.Should().NotThrowAsync();
        this.context.DatasyncDeltaTokens.SingleOrDefault().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SetDeltaTokenAsync_Updates_WhenPresent()
    {
        this.context.DatasyncDeltaTokens.Add(new DatasyncDeltaToken() { Id = "abc", Value = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds() });
        this.context.SaveChanges();

        DateTimeOffset value = DateTimeOffset.UtcNow;
        DatasyncDeltaToken expected = new() { Id = "abc", Value = value.ToUnixTimeMilliseconds() };
        Func<Task> act = async () =>
        {
            await DeltaTokenStore.SetDeltaTokenAsync("abc", value);
            this.context.SaveChanges();
        };

        await act.Should().NotThrowAsync();
        this.context.DatasyncDeltaTokens.SingleOrDefault().Should().BeEquivalentTo(expected);
    }
}
