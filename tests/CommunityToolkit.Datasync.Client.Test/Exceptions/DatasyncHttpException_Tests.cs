// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Exceptions;

[ExcludeFromCodeCoverage]
public class DatasyncHttpException_Tests
{
    [Fact]
    public async Task CreateAsync_NoContentType()
    {
        HttpContent content = new StringContent("some content");
        content.Headers.ContentType = null;
        HttpResponseMessage response = new(HttpStatusCode.BadRequest) { Content = content };

        DatasyncHttpException actual = await DatasyncHttpException.CreateAsync(response);

        actual.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        actual.ContentType.Should().BeEmpty();
        actual.Payload.Should().Be("some content");
    }
}
