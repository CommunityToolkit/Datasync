# NSwag support

Follow [the basic instructions for NSwag integration](https://github.com/RicoSuter/NSwag/wiki/AspNetCore-Middleware), then modify as follows:

1. Add packages to your project to support NSwag.  The following packages are required:

    * [NSwag.AspNetCore](https://www.nuget.org/packages/NSwag.AspNetCore).
    * [CommunityToolkit.Datasync.Server.NSwag](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.NSwag).

2. Add the following to the top of your `Program.cs` file:

        using CommunityToolkit.Datasync.Server.NSwag;

3. Add a service to generate an OpenAPI definition to your `Program.cs` file:

        builder.Services.AddOpenApiDocument(options =>
        {
            options.AddDatasyncProcessors();
        });

4. Enable the middleware for serving the generated JSON document and the Swagger UI, also in `Program.cs`:

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUI3();
        }

Browsing to the `/swagger` endpoint of the web service allows you to browse the API.  The OpenAPI definition can then be imported into other services (such as Azure API Management).  For more information on configuring NSwag, see [Get started with NSwag and ASP.NET Core](https://learn.microsoft.com/aspnet/core/tutorials/getting-started-with-nswag).
