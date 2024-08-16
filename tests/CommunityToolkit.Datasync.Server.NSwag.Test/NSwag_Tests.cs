// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

using CommunityToolkit.Datasync.Server.NSwag.Test.Service;
using CommunityToolkit.Datasync.TestCommon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using NSwag;
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

    [Fact]
    public void ContainsRequestHeader_ReturnsFalse_WhenQueryParam()
    {
        OpenApiOperation sut = new();
        sut.AddODataQueryParameters();

        // Something that doesn't exist.
        sut.ContainsRequestHeader("X-DOES-NOT-EXIST").Should().BeFalse();

        // Something that exists as a query parameter.
        sut.ContainsRequestHeader("$count").Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(TC1))]
    [InlineData(typeof(TC2))]
    [InlineData(typeof(TC3))]
    [InlineData(typeof(TC4<TodoItem>))]
    [InlineData(typeof(TC5))]
    public void IsTableController_ReturnsFalse_ForConditions(Type sut)
    {
        DatasyncOperationProcessor.IsTableController(sut).Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(TC1))]
    [InlineData(typeof(TC2))]
    public void GetTableEntityType_Throws_Properly(Type sut)
    {
        Action act = () => _ = DatasyncOperationProcessor.GetTableEntityType(sut);
        act.Should().Throw<ArgumentException>();
    }

    class TC1 { }
    class TC2 : ControllerBase { }
    abstract class TC3 : TableController<TodoItem> { }
    abstract class TC4<T> : ControllerBase { }
    class TC5 : TC4<TodoItem> { }
}