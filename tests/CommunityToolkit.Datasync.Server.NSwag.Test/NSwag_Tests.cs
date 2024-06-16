// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.NSwag.Test.Service;
using CommunityToolkit.Datasync.TestCommon;
using Microsoft.AspNetCore.TestHost;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Datasync.Server.NSwag.Test;

[ExcludeFromCodeCoverage]
public class NSwag_Tests
{
    private readonly TestServer server = NSwagServer.CreateTestServer();

    [Fact]
    public async Task NSwag_GeneratesSwagger()
    {
        HttpClient client = this.server.CreateClient();
        string actualContent = (await client.GetStringAsync("swagger/v1/swagger.json")).NormalizeContent();
        string expectedContent = Assembly.GetExecutingAssembly().ReadExternalFile("swagger.json");

        // There is an x-generator field that is library specific and completely irrelevant
        // to the comparison, so this line will remove it for comparison purposes.
        Regex generatorRegex = new("\"x-generator\": \"[^\\\"]+\",");
        actualContent = generatorRegex.Replace(actualContent, "", 1);
        expectedContent = generatorRegex.Replace(expectedContent, "", 1);

        // If the expected content is different, it is really hard to diagnose why.
        // Likelihood is that NSwag changed the formatting, and you just need to
        // copy the new file into the swagger.json file location.  However, do an
        // actual diff and open up with a Swagger Editor before you just assume this
        // is the case.
        if (expectedContent != actualContent)
        {
            string outputPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            using StreamWriter output = new(Path.Combine(outputPath, "nswag.out.json"));
            output.Write(actualContent);
            output.Flush();
            output.Close();
        }

        actualContent.Should().Be(expectedContent);
    }
}