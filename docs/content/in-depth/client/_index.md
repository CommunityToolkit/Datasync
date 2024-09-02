+++
title = "Client application"
weight = 20
+++

This guide shows you how to perform common scenarios using the Datasync Community Toolkit.  Use the client library in any .NET 8 application, including AvaloniaUI, MAUI, Uno Platform, WinUI, and WPF applications.

This guide primary deals with offline operations.  For online operations, see the [Online operations guide](./online-operations.md).

## Creating an offline-capable DbContext

Use the `OfflineDbContext` as the base for your offline storage:

```csharp
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
```

> [!NOTE]
> Sqlite stores DateTimeOffset using a second accuracy by default. The Datasync Community Toolkit does not rely on the storage of the `UpdatedAt` field, but it is transmitted with millisecond accuracy.  Consider using [a ValueConverter](https://learn.microsoft.com/ef/core/modeling/value-conversions?tabs=data-annotations) to store the value as a `long` value instead.

Each synchronizable entity in an offline context **MUST** have the following properties:

* `Id` - string, primary key - the globally unique ID for the entity.
* `UpdatedAt` - `DateTimeOffset?` - the date/time that the entity was last updated on the service.
* `Version` - `string?` or `byte[]?` - the opaque version for the entity on the service - changes on each write.
* `Deleted` - boolean (optional) - only needed if using soft-delete on the service; marks the entity as deleted.

> [!TIP]
> **DO NOT USE THE SAME TYPE FOR BOTH SERVICE AND CLIENT**
>
> You may be tempted to use the same entity type for both service and client.  This is a mistake:
> 
> * The service side entity types have automatic updates configured on UpdatedAt and Version which are not appropriate for the client.
> * There are normally properties on the type that should not be transmitted to the client.
>
> While it is possible to use the same entity type for both service and client, ensure you are using repository updating instead of database updating of the metadata properties before considering this.

The `OnDatasyncInitialization()` method is used to set options for data synchronization.  These include setting up the HTTP client and setting up information about each entity.

### Configuring the HttpClient

There are four ways to configure a HttpClient for communication with the datasync service:

1. Use an Endpoint

   ```csharp
   protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
   {
     optionsBuilder.UseEndpoint(new Uri("https://YOURSITEHERE.azurewebsites.net/"));
   }
   ```

2. Use a pre-configured HttpClient

   ```csharp
   protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
   {
     optionsBuilder.UseHttpClient(Services.GetHttpClient());
   }
   ```

3. Use a pre-configured `IHttpClientFactory` - you can use the same one as for online access:

   ```csharp
   protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
   {
     optionsBuilder.UseHttpClientFactory(Services.GetHttpClientFactory());
   }
   ```

4. Use a set of `HttpClientOptions` - the same options that are used in the online access:

   ```csharp
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
   ```

> [!TIP]
> You can easily set up basic and bearer authentication when using `HttpClientOptions` using the `GenericAuthenticationProvider`.
> See the [authentication guide](./auth.md) for more details.

You must configure one of these options so that the data synchronization services know which datasync service to communicate with.

### Configuring entities to synchronize

In addition to configuring the HTTP communication, you can configure each entity to be synchronized:

```csharp
  protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
  {
    options.UseEndpoint(new Uri("https://MYENDPOINT.azurewebsites.net"));

    options.Entity<Movie>(cfg => {
      cfg.ClientName = "movies";
      cfg.Endpoint = new Uri("/api/movies", UriKind.Relative),
      cfg.Query.Where(x => x.Rating != MovieRating.R)
    });
  }
```

This example shows all of the options that can be configured for an entity:

* The `ClientName` is used if you specified an `IHttpClientFactory` and is used when getting a client to use for communication.
* The `Endpoint` can be relative or absolute.  If relative, it is relative to the `BaseAddress` of the `HttpClient` that is used.
* The `Query` limits which entities are requested from the remote service.

## Local only entities

You can specify that a dataset is not to be synchronized by using the `[DoNotSynchronize]` attribute:

```csharp
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

## Pushing data to the remote service

As each entity in the offline store is saved to the database, the changes are added to an operations queue.

> [!TIP]
> You can see what is in the operations queue by accessing the `DatasyncOperationsQueue` dataset on your context.  However, you should treat this data as read-only.

You can push entities to the remote service using the `.PushAsync()` method:

```csharp
// Push pending operations for all synchronizable entities
PushResult pushResult = await context.PushAsync();

// Push pending operations for a subset of synchronizable entities
PushResult pushResult = await context.PushAsync([ typeof(Entity1), typeof(Entity2) ]);

// Push pending operations for a single synchronizable entity.
PushResult pushResult = await context.Movies.PushAsync();
```

In addition, you can specify a `PushOptions` value, with the following properties:

* `ParallelOperations` - (range: 1-8, default: 1) the number of concurrent HTTP requests to use when pushing entities.

If more than one parallel operation is used, the operations MAY be executed on the remote service out of order.

When the push result is complete, the `PushResult` is returned.  This has the following properties:

* `CompletedOperations` - the number of operations that were completed successfully.
* `IsSuccessful` - a boolean to indicate that the push was completed with no errors.
* `FailedRequests` - a `Dictionary<Uri, ServiceResponse>` that indicates which requests failed.

In addition, the operations queue is updated.  Completed operations are removed and failed operations are marked as failed.  You can use the `FailedRequests` property to see the exact error that was returned by the service.

## Pulling data from the service

As with push operations, there are many ways of pulling data from the service. For most situations, you can specify a single filter when configuring the datasync service in `OnDatasyncInitialization` and then use one of the following methods:

```csharp
// Pull changes for all synchronizable entities
PullResult pullResult = await context.PullAsync();

// Pull changes for a subset of synchronizable entities
PullResult pullResult = await context.PullAsync([ typeof(Entity1), typeof(Entity2) ]);

// Pull changes for a single synchronizable entity.
PullResult pullResult = await context.Movies.PullAsync();
```

As with the push operations, you can optionally specify a `PullOptions` object for configuring the transfer:

* `ParallelOperations` specifies how many entity types are pulled in parallel.
* `SaveAfterEveryServiceRequest` ensures `SaveChanges()` is called after every service request (default); otherwise it is only called at the end of the pull operation.

> [!NOTE]
> The context is not thread-safe.  For that reason, service requests are done in parallel, but database updates are done sequentially.  This is normally not a problem because the service requests take much longer than the local database updates.

Service requests are paged and each page contains a link to the next page of results. This means that consecutive service requests for a single entity are serialized, but service requests for different entities can be parallelized.  After each service request, the delta-token for the request is updated.  The delta-token is stored in the `DatasyncDeltaTokens` DbSet on the database context.  For normal requests, the Id is the full name of the entity type.  If a query ID is used (see below), then the Id is `q-<entityType>-<queryId>`.  The `Value` property of the delta-token is the maximum value of the `UpdatedAt` property for the entity data set, expressed as the number of milliseconds since the UNIX epoch.

If you need to pull data from the same entity using different searches, you need to use a pull configurator.

```csharp
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
```

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

```csharp
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
```