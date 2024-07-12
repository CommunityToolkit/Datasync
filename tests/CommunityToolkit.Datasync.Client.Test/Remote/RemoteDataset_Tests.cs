// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Remote;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Tests : BaseOperationTest
{
    [Fact]
    public async Task GetJsonContentFromResponseAsync_InvalidMediaType()
    {
        HttpContent content = new StringContent("this is a test");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("invalid/binary");
        Func<Task> act = () => _ = Dataset.GetJsonContentFromResponseAsync<ClientMovie>(content);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    public async Task GetJsonContentFromResponseAsync_Null()
    {
        HttpContent content = new StringContent("null");
        Func<Task> act = () => _ = Dataset.GetJsonContentFromResponseAsync<ClientMovie>(content);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    public void BuildQueryString_Empty()
    {
        string actual = RemoteDataset<ClientMovie>.BuildQueryString("", "");
        actual.Should().BeEmpty();
    }
}
