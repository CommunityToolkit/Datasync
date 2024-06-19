// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
public class DatasyncClientOptions_Tests
{
    [Fact]
    public void Ctor_Defaults()
    {
        DatasyncClientOptions sut = new();
        sut.DatasyncServiceOptions.Should().NotBeNull();
        sut.HttpClientFactory.Should().BeNull();
        sut.HttpClientName.Should().BeEmpty();
        sut.ParallelOperations.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(8);
        sut.EntityIdGenerator("tableName").Should().NotBeNullOrWhiteSpace();
        sut.TableEndpointResolver("tableName").Should().Be("/tables/tablename");
    }

    [Theory, CombinatorialData]
    public void ParallelOperations_RangeTest([CombinatorialRange(1, 8)] int v)
    {
        Action act = () => _ = new DatasyncClientOptions() { ParallelOperations = v };
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(100)]
    public void ParallelOperations_InvalidRangeTest(int v)
    {
        Action act = () => _ = new DatasyncClientOptions() { ParallelOperations = v };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
