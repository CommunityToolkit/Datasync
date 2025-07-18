# Creating offline clients

This guide shows you how to perform common scenarios using the Datasync Community Toolkit.  Use the client library in any .NET 9 application, including AvaloniaUI, MAUI, Uno Platform, WinUI, and WPF applications.

!!! note **Blazor WASM and Blazor Hybrid**
    The offline capabilities are known to have issues with Blazor WASM and Blazor Hybrid (since EF Core and SQLite do not work in those environments when running in the browser).  Use online-only operations in these environments.  For more information, see [our guide on Blazor WASM](./advanced/blazor-wasm.md)

This guide primary deals with offline operations.  For online operations, see the [Online operations guide](./online.md).

## Creating an offline-capable DbContext

Use the `OfflineDbContext` as the base for your offline storage:

    public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
    {
        public DbSet<TodoItem> TodoItems => Set<TodoItem>();

        protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
        {
          HttpClientOptions clientOptions = new()
          {
              Endpoint = new Uri("https://YOURSITEHERE.azurewebsites.net/"),
              HttpPipeline = [new LoggingHandler()]
          };
          _ = optionsBuilder.UseHttpClientOptions(clientOptions);
        }
    }

!!! warning
    Sqlite stores DateTimeOffset using a second accuracy by default. We strongly recommend using [a ValueConverter](https://learn.microsoft.com/ef/core/modeling/value-conversions?tabs=data-annotations) to store date/time values.

    The Datasync Community Toolkit does not rely on the storage of the `UpdatedAt` field in your model for synchronization.  

Each synchronizable entity in an offline context **MUST** have the following properties:

* `Id` - string, primary key - the globally unique ID for the entity.
* `UpdatedAt` - `DateTimeOffset?` - the date/time that the entity was last updated on the service.
* `Version` - `string?` or `byte[]?` - the opaque version for the entity on the service - changes on each write.
* `Deleted` - boolean (optional) - only needed if using soft-delete on the service; marks the entity as deleted.

!!! warning DO NOT USE THE SAME ENTITY TYPE FOR BOTH SERVICE AND CLIENT
    You may be tempted to use the same entity type for both service and client.  This is a mistake:

    * The service side entity types have automatic updates configured on UpdatedAt and Version which are not appropriate for the client.
    * There are normally properties on the type (such as authorization information) that should not be transmitted to the client.

    If you decide to use the same entity type for both service and client, ensure you use repository updating of the metadata properties. Do **NOT** allow the database to do automatic updates of the entity properties on the client side.

The `OnDatasyncInitialization()` method is used to set options for data synchronization.  These include setting up the HTTP client and setting up information about each entity.

### Configuring the HttpClient

There are four ways to configure a HttpClient for communication with the datasync service:

1. Use an Endpoint:

        protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
        {
          optionsBuilder.UseEndpoint(new Uri("https://YOURSITEHERE.azurewebsites.net/"));
        }

2. Use a pre-configured HttpClient:

        protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
        {
          optionsBuilder.UseHttpClient(Services.GetHttpClient());
        }

3. Use a pre-configured `IHttpClientFactory` - you can use the same one as for online access:

        protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
        {
          optionsBuilder.UseHttpClientFactory(Services.GetHttpClientFactory());
        }

4. Use a set of `HttpClientOptions` - the same options that are used in the online access:

        protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
        {
            HttpClientOptions clientOptions = new()
            {
              Endpoint = new Uri("https://MYENDPOINT.azurewebsites.net"),
              HttpPipeline = [
                new AuthenticatationDelegatingHandler(),
                new LoggingHandler(),
                new ApiKeyRequestHandler("X-API-Key", "my-api-key"),
                new CustomHttpClientHandler()
              ],
              Timeout = TimeSpan.FromSeconds(120)
            };
            optionsBuilder.UseHttpClientOptions(clientOptions);
        }

!!! tip
    You can easily set up basic and bearer authentication when using `HttpClientOptions` using the `GenericAuthenticationProvider`.  See the [authentication guide](./auth.md) for more details.

You must configure one of these options so that the data synchronization services know which datasync service to communicate with.

### Configuring entities to synchronize

In addition to configuring the HTTP communication, you can configure each entity to be synchronized:

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseEndpoint(new Uri("https://MYENDPOINT.azurewebsites.net"));

        optionsBuilder.Entity<Movie>(cfg => {
            cfg.ClientName = "movies";
            cfg.Endpoint = new Uri("/api/movies", UriKind.Relative),
            cfg.Query.Where(x => x.Rating != MovieRating.R)
        });
    }

This example shows all of the options that can be configured for an entity:

* The `ClientName` is used if you specified an `IHttpClientFactory` and is used when getting a client to use for communication.
* The `Endpoint` can be relative or absolute.  If relative, it is relative to the `BaseAddress` of the `HttpClient` that is used.
* The `Query` limits which entities are requested from the remote service.

### Configuring automatic conflict resolution

By default, the library does not do conflict resolution automatically.  You can set an automated conflict resolver by writing an `IConflictResolver` or `IConflictResolver<T>` implementation.  The library provides two by default:

* `ClientWinsConflictResolver` will force-write the client version to the server.
* `ServerWinsConflictResolver` will replace the client version with the server version.

You can set the conflict resolver in two ways - per-entity or as a fallback default:

```csharp
protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder builder)
{
  // A fallback default for cases when you did not set one per entity
  builder.UseDefaultConflictResolver(new ClientWinsConflictResolver());

  // Set a specific conflict resolver for an entity.
  builder.Entity<Movie>(cfg => {
    cfg.ConflictResolver = new ServerWinsConflictResolver();
    // Along with any other settings you want to use
  })
}
```

## Local only entities

You can specify that a dataset is not to be synchronized by using the `[DoNotSynchronize]` attribute:

    public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
    {
        public DbSet<TodoItem> TodoItems => Set<TodoItem>();

        [DoNotSynchronize]
        public DbSet<LocalData> LocalDataset => Set<LocalData>();

        protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
        {
          optionsBuilder.UseEndpoint(new Uri("https://YOURSITEHERE.azurewebsites.net/"));
        }
    }

You cannot (currently) configure an entity such that a property is not synchronized.  Marking the property as `[JsonIgnore]` will prevent the property from being sent to the server, but the property will be overwritten when the entity is pulled from the server.

## Pushing data to the remote service

As each entity in the offline store is saved to the database, the changes are added to an operations queue.

!!! tip
    You can see what is in the operations queue by accessing the `DatasyncOperationsQueue` dataset on your context.  However, you should treat this data as read-only.

You can push entities to the remote service using the `.PushAsync()` method:

    // Push pending operations for all synchronizable entities
    PushResult pushResult = await context.PushAsync();

    // Push pending operations for a subset of synchronizable entities
    PushResult pushResult = await context.PushAsync([ typeof(Entity1), typeof(Entity2) ]);

    // Push pending operations for a single synchronizable entity.
    PushResult pushResult = await context.Movies.PushAsync();

In addition, you can specify a `PushOptions` value, with the following properties:

* `ParallelOperations` - (range: 1-8, default: 1) the number of concurrent HTTP requests to use when pushing entities.

!!! warning
    If more than one parallel operation is used, the operations MAY be executed on the remote service out of order.

When the push result is complete, the `PushResult` is returned.  This has the following properties:

* `CompletedOperations` - the number of operations that were completed successfully.
* `IsSuccessful` - a boolean to indicate that the push was completed with no errors.
* `FailedRequests` - a `Dictionary<string, ServiceResponse>` that indicates which requests failed.

In addition, the operations queue is updated.  Completed operations are removed and failed operations are marked as failed.  You can use the `FailedRequests` property to see the exact error that was returned by the service.

### Conflict resolution

When a conflict resolver is configured, that will be used before a queued change is marked as failed.  In the case of a failed request, you can process the failed requests as follows:

```csharp
foreach (var failedRequest in result.FailedRequests)
{
  var operationId = failedRequest.Key;
  var serviceResponse = failedRequest.Value;

  DatasyncOperation operation = context.DatasyncOperationsQueue.Single(x => x.Id == operationId);
  // operation.EntityType is the type of entity being transferred
  // operation.Item is the JSON-serialized client-side entity
  // operation.EntityVersion is the version of the entity that should be overwritten
  // serviceResponse.ContentStream is the JSON-serialized server-side entity
}
```

Handling conflicts is complex and involves modifying the queue entity and/or client-side entity to match requirements.   Use conflict resolvers in preference of these manual techniques.  A conflict resolver is an implementation of `IConflictResolver` or `IConflictResolver<T>` that is attached to the push operation.  The main method is `ResolveConflictAsync()`.  For example, let's look at the "client-wins" conflict resolver:

```csharp
public class ClientWinsConflictResolver : IConflictResolver
{
    /// <inheritdoc />
    public async Task<ConflictResolution> ResolveConflictAsync(object? clientObject, object? serverObject, CancellationToken cancellationToken = default)
    {
        return new ConflictResolution { Result = ConflictResolutionResult.Client, Entity = clientObject };
    }
}
```

The `IConflictResolver<T>` is the same as `IConflictResolver` with the notable exception that the `clientObject` and `serverObject` are typed instead of objects.  The `ConflictResolution` result model consists of two parts:

* `Result` is either `ConflictResolutionResult.Client` (indicating that the client wins and the server entity should be overwritten) or `ConflictResolutionResult.Server` (indicating that the server wins and the client entity should be overwritten).
* `Entity` is the entity that should be written.

To provide another example, let's say you want to allow updates from the client for all columns except for a `Title` column.  You can do this as follows:

```csharp
public class CustomConflictResolver : IConflictResolver<Movie>
{
  public async Task<ConflictResolution> ResolverConflictAsync(Movie? clientObject, Movie? serverObject, CancellationToken cancellationToken = default)
  {
    clientObject.Movie = serverObject.Movie;
    return new ConflictResolution { Result = ConflictResolutionResult.Client, Entity = clientObject };
  }
}
```

Here, we copy the server value of the movie title to the client before returning so that the title is preserved.

## Pulling data from the service

As with push operations, there are many ways of pulling data from the service. For most situations, you can specify a single filter when configuring the datasync service in `OnDatasyncInitialization` and then use one of the following methods:

    // Pull changes for all synchronizable entities
    PullResult pullResult = await context.PullAsync();

    // Pull changes for a subset of synchronizable entities
    PullResult pullResult = await context.PullAsync([ typeof(Entity1), typeof(Entity2) ]);

    // Pull changes for a single synchronizable entity.
    PullResult pullResult = await context.Movies.PullAsync();

As with the push operations, you can optionally specify a `PullOptions` object for configuring the transfer:

* `ParallelOperations` specifies how many entity types are pulled in parallel.
* `SaveAfterEveryServiceRequest` ensures `SaveChanges()` is called after every service request (default); otherwise it is only called at the end of the pull operation.

!!! warning
    The context is not thread-safe.  For that reason, service requests are done in parallel, but database updates are done sequentially.  This is normally not a problem because the service requests take much longer than the local database updates.

Service requests are paged and each page contains a link to the next page of results. This means that consecutive service requests for a single entity are serialized, but service requests for different entities can be parallelized.  After each service request, the delta-token for the request is updated.  The delta-token is stored in the `DatasyncDeltaTokens` DbSet on the database context.  For normal requests, the Id is the full name of the entity type.  If a query ID is used (see below), then the Id is `q-<entityType>-<queryId>`.  The `Value` property of the delta-token is the maximum value of the `UpdatedAt` property for the entity data set, expressed as the number of milliseconds since the UNIX epoch.

If you need to pull data from the same entity using different searches, you need to use a pull configurator.

    PullResult result = await context.PullAsync(cfg => {
      cfg.SetParallelOperations(2);

      cfg.AddPullRequest<Movie>(); // Use default options

      cfg.AddPullRequest<Movie>(options => {
        options.Endpoint = new Uri("/tables/movies", UriKind.Relative),
        options.HttpClient = myHttpClient,
        options.Query.Where(x => x.Rating == MovieRating.R),
        options.QueryId = "r-rated-movies"
      });
    });

The first example uses the default options as established in the `OnDatasyncInitialization()` method.  The second example shows all the values you can specify:

* The `Endpoint` can be a relative (to the `HttpClient.BaseAddress`) or absolute URI.
* The `HttpClient` can be customized.
* The `Query` can be specified. By default, all entities in the table are pulled.
* The `QueryId` is used to store the delta-token used in incremental sync.  It is generated if not provided.

## Re-synchronizing all data

To re-synchronize all the data in a table, you must:

* Remove all pending operations from the operations queue.
* Remove any delta-tokens in the delta-token store.
* Pull the new data from the remote service.

The following code can be used as an example for this:

    public async Task ResynchronizeAsync(AppDbContext context, Type entityType)
    {
      List<DatasyncOperation> pendingOperations = await context.DatasyncOperationsQueue.Where(x => x.EntityType == entityType.FullName!).ToListAsync();
      context.RemoveRange(pendingOperations);

      List<DatasyncDeltaToken> deltaTokens = await context.DatasyncDeltaTokens.Where(x => x.Id.Contains(entityType.FullName!)).ToListAsync();
      context.RemoveRange(deltaTokens);

      await context.SaveChangesAsync();

      PullResult pullResult = await context.PullAsync([ entityType ]);
      if (!pullResult.IsSuccessful) 
      {
        // Deal with any errors
      }
    }
