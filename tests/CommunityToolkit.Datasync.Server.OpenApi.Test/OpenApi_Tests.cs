// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc.Testing;

namespace CommunityToolkit.Datasync.Server.OpenApi.Test;

[ExcludeFromCodeCoverage]
public class OpenApi_Tests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string OpenApiPath = "/openapi/v1.json";

    [Fact]
    public async Task OpenApiDocument_IsValid()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(OpenApiPath);

        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }
}
