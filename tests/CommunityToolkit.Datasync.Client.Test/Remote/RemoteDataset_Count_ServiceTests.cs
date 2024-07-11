// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Count_ServiceTests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Fact]
    public async Task CountAsync_RetrievesCount()
    {
        long count = await MovieDataset.CountAsync("", DefaultOperationOptions);
        count.Should().Be(TestData.Movies.MovieList.Length);
    }

    [Fact]
    public async Task CountAsync_RetrievesFilteredCount()
    {
        long count = await MovieDataset.CountAsync("$filter=((year gt 2000) or (year lt 1940))", DefaultOperationOptions);
        count.Should().Be(78);
    }
}
