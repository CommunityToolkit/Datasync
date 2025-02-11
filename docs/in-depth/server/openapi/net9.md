# .NET 9.x OpenApi support

Follow [the basic instructions for OpenApi integration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio), then modify as follows:

1. Add packages to your project to support NSwag.  The following packages are required:

    * [Microsoft.AspNetCore.OpenApi](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi).
    * [CommunityToolkit.Datasync.Server.OpenApi](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.OpenApi).

2. Add the following to the top of your `Program.cs` file:

        using CommunityToolkit.Datasync.Server.OpenApi;

3. Add a service to generate an OpenAPI definition to your `Program.cs` file:

        builder.Services.AddOpenApi(options => options.AddDatasyncTransformers());

4. Enable the middleware for serving the generated JSON document and the Swagger UI, also in `Program.cs`:

        app.MapOpenApi();

5. Decorate each table controller with the following attribute:

        [ApiExplorerSettings(IgnoreApi = false)]

Browsing to the `/openapi/v1.json` endpoint of the web service allows you to download the API.  The OpenAPI definition can then be imported into other services (such as Azure API Management).

## Known issues

The .NET 9.x OpenApi support currently does not support dynamic schema generation.  This means that the schema generated within the OpenApi document will be incomplete.