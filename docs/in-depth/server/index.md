# Building a datasync server

A datasync service is a set of RESTful Web API controllers that implement "CRUD" (Create-Read-Update-Delete) with an OData v4 based search capability.  Each table controller serves one entity that must conform to the requirements needed for offline usage.

Because the datasync service is designed to provide disconnected access over intermittent connections, the following should be noted:

* Entity relationships are not supported "across the wire".
* Embedded entities (e.g. objects within JSON objects) are not supported.

## Supported Platforms

The ASP.NET Core packages support ASP.NET 10.0 or later.

The datasync service support a number of backend database servers, and allows for the custom implementation of repositories.  Each entity that is synchronized must meet the following criteria:

* A string-style primary key.
* An automatically updated timestamp with millisecond accuracy.
* An automatically updated version.

Default repository implementations are provided for Entity Framework Core, LiteDB, an in-memory store, and an Automapper store.  For specific database support, see the following:

* Azure Cosmos DB ([EFCore](./db/cosmos.md) and [SDK](./db/cosmos-sdk.md))
* [Azure SQL and SQL Server](./db/azuresql.md)
* [In Memory Datastore](./db/in-memory.md)
* [LiteDb](./db/litedb.md)
* [MongoDb](./db/mongodb.md)
* [MySQL](./db/mysql.md)
* [PostgreSQL](./db/pgsql.md)
* [Sqlite](./db/sqlite.md)

A datasync server based on the Datasync Community Toolkit supports:

* The Datasync Community Toolkit client libraries v8.0.0 and above.
* Azure Mobile Apps client libraries v6.0.0 and above.

## Create a new datasync service

To create a new datasync service:

1. Create an ASP.NET 10.0 (or later) WebAPI project.
2. Add Entity Framework Core 10.0 (or later).
3. Add Datasync Community Toolkit.

For information on creating an ASP.NET Core service with Entity Framework Core, see [the official tutorial](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api).

To enable datasync services, add the following NuGet libraries to your project:

* [CommunityToolkit.Datasync.Server](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server)

Add the support NuGet for one of the supported database technologies.  Refer to the database pages for information.  For example, you would add  [CommunityToolkit.Datasync.Server.EntityFrameworkCore](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.EntityFrameworkCore) for Entity Framework Core based tables.

Modify `Program.cs` and add the following line under all other service definitions:

    builder.Services.AddDatasyncServices();
    builder.Services.AddControllers();

Ensure you map the controllers when setting up your HTTP middleware pipeline.  You can see a full example in the [Test Service](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestService/Program.cs).

## Create a model entity class

All model classes must implement `ITableData`.  Each repository type has an abstract class that implements `ITableData` and provides additional functionality to the repository.  For example, the Entity Framework Core repository provides:

* `InMemoryTableData` for an in-memory data store.
* `LiteDbTableData` for a LiteDb based store.
* `CosmosEntityTableData` for Cosmos Db support via EF Core.
* `CosmosTableData` for Cosmos Db support via the Cosmos DB SDK.
* `EntityTableData` for most EF-Core based database providers.
* `MongoTableData` for Mongo DB (via the MongoDB SDK).
* `SqliteEntityTableData` for Sqlite.

A typical "TodoItem" entity for PostgreSQL would look like this:

    public class TodoItem : EntityTableData
    {
      public required string Title { get; set; }
      public bool IsComplete { get; set; }
    }

The `ITableData` interfaces provides:

* `Id` - a string that is set to be a globally unique ID for the entity.
* `UpdatedAt` - a `DateTimeOffset?` providing the date/time that the entity was last updated (with millisecond accuracy).
* `Version` - a `byte[]` providing an opaque value that changes on every write.
* `Deleted` - a `bool` that is true if the entity has been marked for deletion but not yet purged.

The Datasync libraries or the database maintains these properties.  Do not modify these properties in your own code.

## Update the DbContext

Each model in the database must be registered in the `DbContext`.  For example:

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
      public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    }

## Create a table controller

A table controller is a specialized ApiController that provides the CRUD operations.  Here is a minimal table controller:

    [Route("tables/[controller]")]
    public class TodoItemController : TableController<TodoItem>
    {
      public TodoItemController(AppDbContext context) : base()
      {
        Repository = new EntityTableRepository<TodoItem>(context);
      }
    }

* The controller must have a route.  By convention, the client looks for table controllers on a subpath of '/tables', but they can be placed anywhere.  Make sure you are using the `RouteAttribute` from `Microsoft.AspNetCore.Mvc`.  Your routing will appear broken if you are using `Microsoft.AspNetCore.Components.RouteAttribute`.
* The controller must inherit from `TableController<TEntity>`, where `<TEntity>` is an implementation of the `ITableData` implementation for your repository type.
* Assign a repository based on the same type as your model.

### Table controller options

You can configure certain aspects of the controller using `TableControllerOptions`:

    [Route("tables/[controller]")]
    public class TodoItemController : TableController<TodoItem>
    {
      public TodoItemController(AppDbContext context) : base()
      {
        Repository = new EntityTableRepository<TodoItem>(context);
        Options = new TableControllerOptions { PageSize = 25 };
      }
    }

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

The methods are async in case you need to do an extra database lookup to get the correct answer. You can implement the `IAccessControlProvider<T>` interface on the controller, but you still have to pass in the `IHttpContextAccessor` to access the `HttpContext` in a thread safe manner.

To use this access control provider, update your `TableController` as follows:

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

If you want to allow both unauthenticated and authenticated access to a table, decorate the controller with `[AllowAnonymous]` instead of `[Authorize]`.

!!! warning
    It is very easy to accidentally leak data for a different user when using access control providers to limit the data.  If the operation is create and the entity that exists is for another user, the response will contain the information for the other user.

    To prevent this data leakage case, it is important to add a filtering that ensures the data being returned is for the same user.

## Configure logging

Logging is handled through [the normal logging mechanism](https://learn.microsoft.com/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0) for ASP.NET Core.  Assign the `ILogger` object to the `Logger` property:

    [Route("tables/[controller]")]
    public class ModelController : TableController<Model>
    {
        public ModelController(AppDbContext context, Ilogger<ModelController> logger) : base()
        {
            Repository = new EntityTableRepository<Model>(context);
            Logger = logger;
        }
    }

## Monitor repository changes

When the repository is changed, you can trigger workflows, log the response to the client, or do other work in one of two methods:

### Option 1: Implement a PostCommitHookAsync

The `IAccessControlProvider<T>` interface provides a `PostCommitHookAsync()` method.  Th `PostCommitHookAsync()` method is called after the data is written to the repository but before returning the data to the client.  Care must be made to ensure that the data being returned to the client isn't changed in this method.

    public class MyAccessControlProvider<T> : AccessControlProvider<T> where T : ITableData
    {
        public override async Task PostCommitHookAsync(TableOperation op, T entity, CancellationToken cancellationToken = default)
        {
            // Do any work you need to here.
            // Make sure you await any asynchronous operations.
        }
    }

Use this option if you're running asynchronous tasks as part of the hook.

### Option 2: Use the RepositoryUpdated event handler

The `TableController<T>` base class contains an event handler that is called at the same time as the `PostCommitHookAsync()` method.

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

## OpenAPI Support

You can publish the API defined by data sync controllers using [NSwag](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-nswag?view=aspnetcore-10.0&tabs=visual-studio), [Swashbuckle](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-10.0&tabs=visual-studio), or the [OpenApi support in .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0).  In all cases, start by setting up the service as you normally would for the chosen library. 

Review the instructions for each library:

* [NSwag](./openapi/nswag.md)
* [OpenApi](./openapi/net9.md)
* [Swashbuckle](./openapi/swashbuckle.md)

Currently, NSwag provides the best option for OpenApi document generation.
