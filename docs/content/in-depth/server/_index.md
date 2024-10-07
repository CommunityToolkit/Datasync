+++
title = "Datasync Server"
weight = 10
+++

This article shows you how to configure and use the ASP.NET Core backend packages for the Datasync Community Toolkit to produce a datasync service.

A datasync service is a set of RESTful Web API controllers that implement "CRUD" (Create-Read-Update-Delete) with an OData v4 based search capability.  Each table controller serves one entity that must conform to the requirements needed for offline usage.

## Supported Platforms

The ASP.NET Core packages support ASP.NET 8.0 or later.

The datasync service support a number of backend database servers, and allows for the custom implementation of repositories.  Each entity that is synchronized must meet the following criteria:

* A string-style primary key.
* An automatically updated timestamp with millisecond accuracy.
* An automatically updated version.

Default repository implementations are provided for Entity Framework Core, LiteDB, an in-memory store, and an Automapper store.  For specific database support, see the following:

* [Azure Cosmos DB](./databases/cosmos.md)
* [Azure SQL and SQL Server](./databases/azuresql.md)
* [In Memory Datastore](./databases/in-memory.md)
* [LiteDb](./databases/litedb.md)
* [PostgreSQL](./databases/postgresql.md)
* [Sqlite](./databases/sqlite.md)

A datasync server based on the Datasync Community Toolkit supports:

* The Datasync Community Toolkit client libraries v8.0.0 and above.
* Azure Mobile Apps client libraries v6.0.0 and above.

## Create a new datasync service

To create a new datasync service:

1. Create an ASP.NET 8.0 (or later) WebAPI project.
2. Add Entity Framework Core.
3. Add Datasync Community Toolkit.

For information on creating an ASP.NET Core service with Entity Framework Core, see [the official tutorial](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api).

To enable datasync services, add the following NuGet libraries to your project:

* [CommunityToolkit.Datasync.Server](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server)
* [CommunityToolkit.Datasync.Server.EntityFrameworkCore](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.EntityFrameworkCore) for Entity Framework Core based tables.
* [CommunityToolkit.Datasync.Server.LiteDb](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.LiteDb) for LiteDb based tables.

Modify `Program.cs` and add the following line under all other service definitions:

```csharp
builder.Services.AddDatasyncServices();
builder.Services.AddControllers();
```

Ensure you map the controllers when setting up your HTTP middleware pipeline.  You can see a full example in the [Test Service](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestService/Program.cs).

## Create a model entity class

All model classes must implement `ITableData`.  Each repository type has an abstract class that implements `ITableData` and provides additional functionality to the repository.  For example, the Entity Framework Core repository provides:

* `SqliteEntityTableData` for Sqlite support.
* `CosmosEntityTableData` for Cosmos Db support.
* `EntityTableData` for other database services.

A typical "TodoItem" entity for Postgresql would look like this:

```csharp
public class TodoItem : EntityTableData
{
  public required string Title { get; set; }
  public bool IsComplete { get; set; }
}
```

The `ITableData` interfaces provides:

* `Id` - a string that is set to be a globally unique ID for the entity.
* `UpdatedAt` - a `DateTimeOffset?` providing the date/time that the entity was last updated (with millisecond accuracy).
* `Version` - a `byte[]` providing an opaque value that changes on every write.
* `Deleted` - a `bool` that is true if the entity has been marked for deletion but not yet purged.

The Datasync libraries or the database maintains these properties.  Do not modify these properties in your own code.

## Update the DbContext

Each model in the database must be registered in the `DbContext`.  For example:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}
```

## Create a table controller

A table controller is a specialized ApiController that provides the CRUD operations.  Here is a minimal table controller:

```csharp
[Route("tables/[controller]")]
public class TodoItemController : TableController<TodoItem>
{
  public TodoItemController(AppDbContext context) : base()
  {
    Repository = new EntityTableRepository<TodoItem>(context);
  }
}
```

* The controller must have a route.  By convention, the client looks for table controllers on a subpath of '/tables', but they can be placed anywhere.  Make sure you are using the `RouteAttribute` from `Microsoft.AspNetCore.Mvc`.  Your routing will appear broken if you are using `Microsoft.AspNetCore.Components.RouteAttribute`.
* The controller must inherit from `TableController<TEntity>`, where `<TEntity>` is an implementation of the `ITableData` implementation for your repository type.
* Assign a repository based on the same type as your model.

### Table controller options

You can configure certain aspects of the controller using `TableControllerOptions`:

```csharp
[Route("tables/[controller]")]
public class TodoItemController : TableController<TodoItem>
{
  public TodoItemController(AppDbContext context) : base()
  {
    Repository = new EntityTableRepository<TodoItem>(context);
    Options = new TableControllerOptions { PageSize = 25 };
  }
}
```

The options you can set include:

* `PageSize` (int, default: 100) is the maximum number of items a query operation returns in a single page.
* `MaxTop` (int, default: 512000) is the maximum number of items a user can request in a single operation.
* `EnableSoftDelete` (bool, default: false) enables soft-delete, which marks items as deleted instead of deleting them from the database.  Soft delete allows clients to update their offline cache, but requires that deleted items are purged from the database separately.
* `UnauthorizedStatusCode` (int, default: 401 Unauthorized) is the status code returned when the user isn't allowed to do an action.

## Configure access permissions

By default, a user can do anything that they want to entities within a table - create, read, update, and delete any record.  For more fine-grained control over authorization, create a class that implements `IAccessControlProvider`.  This implements three methods to implement per-item authorization:

* `GetDataView()` returns a lambda that limits what the connected user can see.
* `IsAuthorizedAsync()` determines if the connected user can perform the action on the specific entity that is being referenced.
* `PreCommitHookAsync()` adjusts any entity immediately before being written to the repository.

You can effectrively handle most access control cases with these three methods.  If you need access to the `HttpContext`, [configure an HttpContextAccessor](https://learn.microsoft.com/aspnet/core/fundamentals/use-http-context#httpcontext-isnt-thread-safe).

As an example, the following code implements a personal table where the user can only see the entities that they created.

```csharp
public class PrivateAccessControlProvider<T>: IAccessControlProvider<T>
    where T : ITableData
    where T : IUserId
{
    private readonly IHttpContextAccessor _accessor;

    public PrivateAccessControlProvider(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private string UserId { get => _accessor.HttpContext.User?.Identity?.Name; }

    public Expression<Func<T,bool>> GetDataView()
    {
      return (UserId == null)
        ? _ => false
        : model => model.UserId == UserId;
    }

    public Task<bool> IsAuthorizedAsync(TableOperation op, T entity, CancellationToken token = default)
    {
        if (op == TableOperation.Create || op == TableOperation.Query)
        {
            return Task.FromResult(true);
        }
        else
        {
            return Task.FromResult(entity?.UserId != null && entity?.UserId == UserId);
        }
    }

    public virtual Task PreCommitHookAsync(TableOperation operation, T entity, CancellationToken token = default)
    {
        entity.UserId == UserId;
        return Task.CompletedTask;
    }
}
```

The methods are async in case you need to do an extra database lookup to get the correct answer. You can implement the `IAccessControlProvider<T>` interface on the controller, but you still have to pass in the `IHttpContextAccessor` to access the `HttpContext` in a thread safe manner.

To use this access control provider, update your `TableController` as follows:

```csharp
[Authorize]
[Route("tables/[controller]")]
public class ModelController : TableController<Model>
{
    public ModelsController(AppDbContext context, IHttpContextAccessor accessor) : base()
    {
        AccessControlProvider = new PrivateAccessControlProvider<Model>(accessor);
        Repository = new EntityTableRepository<Model>(context);
    }
}
```

If you want to allow both unauthenticated and authenticated access to a table, decorate the controller with `[AllowAnonymous]` instead of `[Authorize]`.

## Configure logging

Logging is handled through [the normal logging mechanism](https://learn.microsoft.com/aspnet/core/fundamentals/logging/?view=aspnetcore-8.0) for ASP.NET Core.  Assign the `ILogger` object to the `Logger` property:

```csharp
[Route("tables/[controller]")]
public class ModelController : TableController<Model>
{
    public ModelController(AppDbContext context, Ilogger<ModelController> logger) : base()
    {
        Repository = new EntityTableRepository<Model>(context);
        Logger = logger;
    }
}
```

## Monitor repository changes

When the repository is changed, you can trigger workflows, log the response to the client, or do other work in one of two methods:

### Option 1: Implement a PostCommitHookAsync

The `IAccessControlProvider<T>` interface provides a `PostCommitHookAsync()` method.  Th `PostCommitHookAsync()` method is called after the data is written to the repository but before returning the data to the client.  Care must be made to ensure that the data being returned to the client isn't changed in this method.

```csharp
public class MyAccessControlProvider<T> : AccessControlProvider<T> where T : ITableData
{
    public override async Task PostCommitHookAsync(TableOperation op, T entity, CancellationToken cancellationToken = default)
    {
        // Do any work you need to here.
        // Make sure you await any asynchronous operations.
    }
}
```

Use this option if you're running asynchronous tasks as part of the hook.

### Option 2: Use the RepositoryUpdated event handler

The `TableController<T>` base class contains an event handler that is called at the same time as the `PostCommitHookAsync()` method.

```csharp
[Authorize]
[Route(tables/[controller])]
public class ModelController : TableController<Model>
{
    public ModelController(AppDbContext context) : base()
    {
        Repository = new EntityTableRepository<Model>(context);
        RepositoryUpdated += OnRepositoryUpdated;
    }

    internal void OnRepositoryUpdated(object sender, RepositoryUpdatedEventArgs e) 
    {
        // The RepositoryUpdatedEventArgs contains Operation, Entity, EntityName
    }
}
```

## OpenAPI Support

You can publish the API defined by data sync controllers using [NSwag](/aspnet/core/tutorials/getting-started-with-nswag) or [Swashbuckle](/aspnet/core/tutorials/getting-started-with-swashbuckle).  In both cases, start by setting up the service as you normally would for the chosen library.  

### NSwag

Follow the basic instructions for NSwag integration, then modify as follows:

1. Add packages to your project to support NSwag.  The following packages are required:

    * [NSwag.AspNetCore](https://www.nuget.org/packages/NSwag.AspNetCore).
    * [CommunityToolkit.Datasync.Server.NSwag](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.NSwag).

2. Add the following to the top of your `Program.cs` file:

    ```csharp
    using CommunityToolkit.Datasync.Server.NSwag;
    ```

3. Add a service to generate an OpenAPI definition to your `Program.cs` file:

    ```csharp
    builder.Services.AddOpenApiDocument(options =>
    {
        options.AddDatasyncProcessors();
    });
    ```

4. Enable the middleware for serving the generated JSON document and the Swagger UI, also in `Program.cs`:

    ```csharp
    if (app.Environment.IsDevelopment())
    {
        app.UseOpenApi();
        app.UseSwaggerUI3();
    }
    ```

Browsing to the `/swagger` endpoint of the web service allows you to browse the API.  The OpenAPI definition can then be imported into other services (such as Azure API Management).  For more information on configuring NSwag, see [Get started with NSwag and ASP.NET Core](https://learn.microsoft.com/aspnet/core/tutorials/getting-started-with-nswag).

### Swashbuckle

Follow the basic instructions for Swashbuckle integration, then modify as follows:

1. Add packages to your project to support Swashbuckle.  The following packages are required:

    * [Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore).
    * [Swashbuckle.AspNetCore.Newtonsoft](https://www.nuget.org/packages/Swashbuckle.AspNetCore.Newtonsoft).
    * [CommunityToolkit.Datasync.Server.Swashbuckle](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.Swashbuckle).

2. Add a service to generate an OpenAPI definition to your `Program.cs` file:

    ```csharp
    builder.Services.AddSwaggerGen(options => 
    {
        options.AddDatasyncControllers();
    });
    builder.Services.AddSwaggerGenNewtonsoftSupport();
    ```

    > [!NOTE]
    > The `AddDatasyncControllers()` method takes an optional `Assembly` that corresponds to the assembly that contains your table controllers.  The `Assembly` parameter is only required if your table controllers are in a different project to the service.

3. Enable the middleware for serving the generated JSON document and the Swagger UI, also in `Program.cs`:

    ```csharp
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => 
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = string.Empty;
        });
    }
    ```

With this configuration, browsing to the root of the web service allows you to browse the API.  The OpenAPI definition can then be imported into other services (such as Azure API Management).  For more information on configuring Swashbuckle, see [Get started with Swashbuckle and ASP.NET Core](https://learn.microsoft.com/aspnet/core/tutorials/getting-started-with-swashbuckle).
