// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Swashbuckle.Test.Service;
using CommunityToolkit.Datasync.TestCommon;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using NSubstitute;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Xunit;

namespace CommunityToolkit.Datasync.Server.Swashbuckle.Test;

[ExcludeFromCodeCoverage]
public class Swashbuckle_Tests
{
    private readonly TestServer server = SwashbuckleServer.CreateTestServer();

    [Fact]
    public async Task Swashbuckle_GeneratesSwagger()
    {
        HttpClient client = this.server.CreateClient();
        string actualContent = (await client.GetStringAsync("swagger/v1/swagger.json")).NormalizeContent();
        string expectedContent = Assembly.GetExecutingAssembly().ReadExternalFile("swagger.json");

        // There is an x-generator field that is library specific and completely irrelevant
        // to the comparison, so this line will remove it for comparison purposes.
        //Regex generatorRegex = new("\"x-generator\": \"[^\\\"]+\",");
        //actualContent = generatorRegex.Replace(actualContent, "", 1);
        //expectedContent = generatorRegex.Replace(expectedContent, "", 1);

        // If the expected content is different, it is really hard to diagnose why.
        // Likelihood is that NSwag changed the formatting, and you just need to
        // copy the new file into the swagger.json file location.  However, do an
        // actual diff and open up with a Swagger Editor before you just assume this
        // is the case.
        if (expectedContent != actualContent)
        {
            string outputPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            using StreamWriter output = new(Path.Combine(outputPath, "swashbuckle.out.json"));
            output.Write(actualContent);
            output.Flush();
            output.Close();
        }

        actualContent.Should().Be(expectedContent);
    }

    [Theory]
    [InlineData(typeof(TC1))]
    [InlineData(typeof(TC2))]
    [InlineData(typeof(TC3))]
    [InlineData(typeof(TC4<TodoItem>))]
    [InlineData(typeof(TC5))]
    public void IsTableController_ReturnsFalse_ForConditions(Type sut)
    {
        DatasyncDocumentFilter.IsTableController(sut).Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(object), false, null)]
    [InlineData(typeof(TC1), false, null)]
    [InlineData(typeof(TC2), false, null)]
    [InlineData(typeof(TC3), true, typeof(TodoItem))]
    [InlineData(typeof(TC4<TodoItem>), false, null)]
    [InlineData(typeof(TC5), true, typeof(TodoItem))]
    public void TryGetTableEntityType_Returns_CorrectValue(Type sut, bool expectedResult, Type expectedEntityType)
    {
        bool actual = DatasyncDocumentFilter.TryGetTableEntityType(sut, out Type actualEntityType);
        actual.Should().Be(expectedResult);
        if (expectedEntityType == null)
        {
            actualEntityType.Should().BeNull();
        }
        else
        {
            actualEntityType.Should().Be(expectedEntityType);
        }
    }

    [Fact]
    public void GetRoutePathFromContext_ReturnsNullAppropriately()
    {
        DocumentFilterContext context = new([], Substitute.For<ISchemaGenerator>(), new SchemaRepository());
        string routePath = DatasyncDocumentFilter.GetRoutePathFromContext(context, typeof(TC3));
        routePath.Should().BeNull();
    }

    [Fact]
    public void GetAllTableController_ThisAssembly()
    {
        string[] expected = [ "KitchenReaderController", "KitchenSinkController", "TodoItemController" ];
        List<string> controllerNames = DatasyncDocumentFilter.GetAllTableControllers(null).Select(x => x.Name).ToList();
        controllerNames.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetAllTableControllers_AlternateAssembly()
    {
        // Adjust this as necessary to the list of controllers in the TestService.
        string[] expected = [
            "AuthorizedMovieController",
            "InMemoryKitchenSinkController",
            "InMemoryMovieController",
            "InMemoryPagedMovieController",
            "InMemorySoftDeletedMovieController"
        ];
        Assembly assembly = typeof(TestService.Controllers.InMemoryMovieController).Assembly;
        List<string> controllerNames = DatasyncDocumentFilter.GetAllTableControllers(assembly).Select(x => x.Name).ToList();
        controllerNames.Should().BeEquivalentTo(expected);
    }

    class TC1 { }
    class TC2 : ControllerBase { }
    abstract class TC3 : TableController<TodoItem> { }
    abstract class TC4<T> : ControllerBase { }
    class TC5 : TC4<TodoItem> { }
}
