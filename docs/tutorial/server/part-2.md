# The standard repositories

In the [previous tutorial](./part-1.md), I covered how to get started with the Datasync Community Toolkit.  In this tutorial, I am going to delve into the standard repositories that can be leveraged to implement database-driven datasync endpoints in your application.

## The Entity Framework Core repository

Introduced in the last tutorial, the [Entity Framework Core][1] repository is the most popular repository because it connects to so many different databases.  There are explicit notes for:

* [Azure SQL][2]
* [Cosmos DB][3]
* [MySql][4]
* [PostgreSql][5]
* [Sqlite][6]

Any database that [has an Entity Framework Core driver][7] should work, although you may need to determine how to set up the database and which style of table data you need.  As mentioned in the previous tutorial, there are four different implementations of `ITableData` to choose from:

* `EntityTableData` is used whenever the server can control `UpdatedAt` and `Version`.
* `CosmosEntityTableData` is used specifically for Cosmos DB.  Cosmos controls `Version` but not `UpdatedAt`.
* `SqliteEntityTableData` is used specifically for Sqlite.  Sqlite does not store `UpdatedAt` with the right resolution, so special care is needed.
* `RepositoryControlledEntityTableData` is used when the server cannot control `UpdatedAt` and `Version` - the repository controls them instead.

Make sure you reference the provided notes before implementing your database tables.

!!! warning Do not use Sqlite on the server side
    The Datasync Community Toolkit relies on `UpdatedAt` being stored with millisecond or better accuracy.  The `DateTime` data type used by Sqlite stores data with second accuracy.  As such, you need to exercise great care when implementing a Sqlite table.

## The In Memory Repository

When I started writing the datasync service, I needed a simple repository based in-memory to use when testing and I didn't want to be guessing if the problem was a problem with my code or a peculiarity of the Entity Framework Core libraries.  I wrote a simple in-memory repository to solve this.  It turns out that in-memory repositories are really useful when the data doesn't change very often, even if the data is stored in a database.  It's also useful for test applications before you add a database for persistent storage.

Let's start with a common situation - you have a set of categories that are used in your application.  The categories don't change much and the user can't change them.  This is an ideal situation for an in-memory repository.  The in-memory repository requires the `CommunityToolkit.Datasync.Server.InMemory` NuGet package.

First, let's define a new class for the model:

```csharp
public class CategoryDTO : InMemoryTableData
{
  [Required, StringLength(64, MinimumLength = 1)]
  public string CategoryName { get; set; } = string.Empty;
}
```

Create an initializer:

```csharp
public interface IDatasyncInitializer
{
  Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class DatasyncInitializer(AppDbContext context, IRepository<CategoryDTO> repository) : IDatasyncInitializer
{
  public async Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    IList<Category> categories = await context.Categories.ToListAsync();
    IList<CategoryDTO> seed = categories.Select(x => Convert(x)).ToList();
    
    IQueryable<CategoryDTO> queryable = await repository.AsQueryableAsync(cancellationToken);
    foreach (CategoryDTO dto in seed) 
    {
      if (!queryable.Any(x => x.CategoryName.Equals(dto.CategoryName, StringComparison.OrdinalIgnoreCase)))
      {
        await repository.CreateAsync(dto, cancellationToken);
      }
    }
  }

  private static CategoryDTO Convert(Category category) => new()
    {
      Id = category.MobileId,
      UpdatedAt = category.UpdatedAt ?? DateTimeOffset.UnixEpoch,
      Version = category.Version ?? Guid.NewGuid().ToByteArray(),
      Deleted = false,
      CategoryName = category.CategoryName
    };
}
```

I've obviously made this a little more complex than it needs to be. I've ensured that the database model is different from the data transfer object (or DTO), so I need to translate between them.

* The database model uses an auto-incrementing ID, but my DTO requires a globally unique ID.  Create a `MobileId` field to solve this.
* The `UpdatedAt` is stored in the database but it might be null. Always ensure that `UpdatedAt` hs a valid value that is more recent than the UNIX epoch.
* Similarly, the `Version` may not be set.  The service (and clients) expect each entity to have a version.
* The database model doesn't have a `Deleted` flag.  It isn't used in this case, but the protocol requires it exist. 

!!! info The meaning of DTO
    The term "DTO" refers to a "Data Transfer Object" - an object that is transferred between client and server.  It is so named to distinguish it from the database object (which normally carries the real entity name).  You will often see `Model` and `ModelDTO` as pairs.

In the `InitializeAsync()` method, the database is queried to get the list of categories, then converted to the version that is sent to clients.

Next, add the repository to the services in `Program.cs`:

```csharp
builder.Services.AddSingleton<IRepository<CategoryDTO>, InMemoryRepository<CategoryDTO>>();
builder.Services.AddScoped<IDatasyncInitializer, DatasyncInitializer>();
```

!!! tip Use a singleton to store data
    The `DbContext` used in the Entity Framework Core is created for each request, thus the repository is also created per-request.  When using an in-memory repository, you want to use the same repository for all requests, so use a singleton.  The in-memory repository is thread-safe.

Initialize the repository immediately after you build the web application.  Since I am using a `DbInitializer` as well, I can include the datasync initializer in the same area.  This is what a typical initialization (immediately after the application is built) looks like:

```csharp
TimeSpan allowedInitializationTime = TimeSpan.FromMinutes(5);
CancellationTokenSource cts = new();

using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    IDbInitializer dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    IDatasyncInitializer datasyncInitializer = scope.ServiceProvider.GetRequiredService<IDatasyncInitializer>();
    cts.CancelAfter(allowedInitializationTime);
    try
    {
        CancellationToken ct = cts.Token;
        await dbInitializer.InitializeAsync(ct);
        await datasyncInitializer.InitializeAsync(ct);
    }
    catch (OperationCanceledException)
    {
        throw new ApplicationException($"Initialization failed to complete within {allowedInitializationTime}");
    }
}
```

Create a datasync table controller that uses the repository:

```csharp
[Route("tables/[controller]")]
public class CategoryController : TableController<CategoryDTO>
{
    public CategoryController(IRepository<CategoryDTO> repository, ILogger<CategoryController> logger)
        : base(repository)
    {
        Logger = logger;
    }
}
```

As I showed in the last tutorial, you can use Swashbuckle to interact with the server.  Note that the `/tables/category` endpoint acts just like the database backed endpoint.  However, data does not get persisted to the database and all data is served from memory.  You can use all the same options as the Entity Framework Core repository. These were introduced in the last tutorial.

!!! info Implementing access control
    You are probably wondering how to make this table "read-only" at this point.  The answer to your question is "Access Control Providers" and I will be covering that topic in depth in [a later tutorial](./part-4.md).

## The Automapper Repository

The next repository type is the [Automapper][8] repository.  Unlike the Entity Framework Core repository and the in-memory repository, this repository wraps another repository; it transforms the data before it is stored and while it is being read from the database.

We've introduced Data Transfer Objects (or DTOs) above.  Separating the database model from the model sent to the client is a commonly required strategy.  Not all properties are relevant to clients, or you need to rename some properties on the way through, or do some data transformation. AutoMapper is very flexible in this regard.

Let's say you are using a database table, but you've recently updated the database table to add the required metadata.  The `UpdatedAt` property may not be set (as required by the protocol).

First, set up your Automapper.

* Install the `Automapper` NuGet package.
* Create a profile.  Here is mine:

    ```csharp
    using AutoMapper;

    namespace InMemoryDatasyncService.Models;

    public class TodoItemProfile : Profile
    {
        public TodoItemProfile()
        {
            CreateMap<TodoItem, TodoItemDTO>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.NullSubstitute(DateTimeOffset.UnixEpoch))
                .ReverseMap();
        }
    }
    ```

    I am using the automapper to automatically set the `UpdatedAt` property if it is not set.

* Install the profile into the services collection within your `Program.cs`:

    ```csharp
    builder.Services.AddAutoMapper(typeof(TodoItemProfile));
    ```

Each entity being auto-mapped needs both a forward and a reverse map.  You should take care to ensure that the conditions required for the datasync metadata are met for each model:

* `UpdatedAt` must be unique within the table and have millisecondsec precision.
* `Version` should be a byte array that changes on every write.
* `Id` must be a globally unique string.  

You can use any of the AutoMapper features, including custom type converters, custom resolvers, and null substitution to ensure that your conversion works properly.

Both the model and the DTO must be "datasync ready" - i.e. they must inherit from something that implements `ITableData`.  The model must be suitable for use with the underlying repository.  Obviously, I am not doing anything particularly challenging in my example.  

!!! tip Test your Automapper profile
    You should always write unit tests for your automapper profile to ensure that the model can be converted to and from a DTO, including when the data is not set.

As always, create a suitable table controller.  The `MappedTableRepository` is in the `CommunityToolkit.Datasync.Server.Automapper` NuGet package.  Here is a typical controller:

```csharp
[Route("api/todoitem")]
public class AutomappedController : TableController<TodoItemDTO>
{
    public AutomappedController(IMapper mapper, AppDbContext context) : base()
    {
        var efrepo = new EntityTableRepository<TodoItem>(context);
        Repository = new MappedTableRepository<TodoItem, TodoItemDTO>(mapper, efrepo);
    }
}
```

I don't find the `MappedTableRepository` that useful since it wraps an existing repository, which means the database model must conform to the datasync standards.  In the [next tutorial](./part-3.md), we'll take a look at custom repositories that don't have this restriction.

## The MongoDB Repository

Among NoSQL data stores, [MongoDB][9] is perhaps the most well-known. NoSQL databases store JSON documents instead of table data.  However, the Datasync Community Toolkit is a table-driven system, so it only works with "flat" JSON documents (without embedded arrays or objects).

!!! tip Avoid NoSQL stores for datasync
    In general, you should avoid NoSQL stores because they don't have the expansive query capabilities of SQL stores.  For example, you can write queries that perform math, string, and date/time operations with SQL, but those capabilities generally don't exist with NoSQL stores.  NoSQL stores are good options when all your processing is done on the client, however.

Like other repositories:

* You must add a NuGet package; in this case `CommunityToolkit.Datasync.Server.MongoDB`.
* Your entities must inherit from `MongoTableData`.

When creating your table controller, you need a reference to the `MongoClient`.  It is relatively easy to exhaust the connections available to MongoDb servers, so you should generally use a singleton client or a connection pool.  For example, here is an example of a simple wrapper of the client:

```csharp
public class MongoClientFactory
{
  private readonly MongoClient client;

  public MongoClientFactory(string connectionString)
  {
    MongoClientSettings clientSettings = MongoClientSettings.FromConnectionString(connectionString);
    this.client = new MongoClient(clientSettings);
  }

  public MongoClient Client { get => this.client; }
}
```

I can set this up as a singleton when setting up all my other services: 

```csharp
builder.Services.AddSingleton<MongoClientFactory>(new MongoClientFactory(builder.Configuration.GetConnectionString("MongoDb")));
```

Now I can create the normal table controller:

```csharp
[Route("tables/[controller]")]
public class EntityController : TableController<Entity>
{
  public EntityController(MongoClientFactory clientFactory, ILogger<EntityController> logger)
  {
    IMongoDatabase database = clientFactory.Client.GetDatabase("synchronized-data");
    Repository = new MongoDBRepository<Entity>(database.GetCollection<Entity>("entities"));
    Logger = logger;
  }
}
```

Unlike NoSQL databases, you cannot store embedded documents.  For example, it's perfectly fine to store the following entity:

```json
{
  "name": "Joe Smith",
  "address": {
    "line1": "1 Main Street",
    "city": "New York City",
    "state": "NY"
  }
}
```

However, this doesn't work with the Datasync Community Toolkit - only "flat" entities are allowed.  In this case, the "address" is an object which does not have a primitive data type, so it is disallowed.

## The LiteDB Repository

Finally, there is a [LitDB](https://www.litedb.org) based repository.  LiteDB is a serverless embedded database that is written entirely in .NET, so it's ideal for cases where you need "something" to be a database but you don't want to go to the effort of setting up a server.  [SQLite](https://sqlite.org/) also fits the bill here.  However, SQLite has some restrictions around data/time handling that make it unsuitable for datasync applications on the server.  LiteDB doesn't have these restrictions. It naturally stores date/times with an ISO-8601 conversion and millisecond accuracy.  The LiteDB can be used "in-memory" on "on-disk".  

Like other repositories:

* You must add a NuGet package; in this case `CommunityToolkit.Datasync.Server.LiteDB`.
* Your entities must inherit from `LiteDbTableData`.

Similar to the MongoDB repository, add the database to the services as a singleton:

```csharp
string liteDBConnectionString = builder.Configuration.GetConnectionString("LiteDB");
builder.Services.AddSingleton<LiteDatabase>(new LiteDatabase(liteDBConnectionString));
```

Then, build your controller:

```csharp
[Route("tables/[controller]")]
public class LiteItemController(LiteDatabase db) : TableController<LiteItem>(new LiteDbRepository<LiteItem>(db, "todoitems"))
{
}
```

## Final thoughts

The Datasync Community Toolkit has five standard repositories:

* AutoMapper
* Entity Framework Core
* In-Memory
* LiteDb
* MongoDB

These cover the majority of situations that you will come across. They get you started fast and efficiently while still being "production ready".

However, there are always those cases when one of the standard repositories doesn't work for you.  The canonical example for such a situation is when you are using an existing database table that uses auto-incrementing integers for the key. The pluggable architecture of the Datasync Community Toolkit also allows you to write your own repositories.  In the [next tutorial](./part-3.md), I'm going to dive into how you do that and how you can use custom repositories for the specific example of handling an existing database table.

<!-- Links -->
[1]: https://learn.microsoft.com/ef/core/
[2]: ../../in-depth/server/db/azuresql.md
[3]: ../../in-depth/server/db/cosmos.md
[4]: ../../in-depth/server/db/mysql.md
[5]: ../../in-depth/server/db/pgsql.md
[6]: ../../in-depth/server/db/sqlite.md
[7]: https://learn.microsoft.com/ef/core/providers/
[8]: https://docs.automapper.org/en/stable/
[9]: https://www.mongodb.com/products/self-managed/community-edition
