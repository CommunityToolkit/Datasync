# Online operations

Not all data needs to be synchronized.  You may want to do an online search of records for a search capability, for example.  To support this, The Datasync Community Toolkit supports an online client in addition to offline usage.

If you are using the Datasync Community Toolkit with Blazor WASM, see [our guide on Blazor WASM usage](./advanced/blazor-wasm.md).

## Creating a Http Client Factory

To create an online client, you must create an `IHttpClientFactory` that creates the appropriate `HttpClient` objects that are used to communicate with the remote service.  This can handle authentication, logging, and anything else that is required by the remote service.  At a minimum, a `BaseAddress` must be established.  To facilitate this, the Datasync Community Toolkit provides a default `HttpClientFactory` that can be used:

```csharp
using CommunityToolkit.Datasync.Client.Http;

public IHttpClientFactory GetClientFactory()
{
  HttpClientOptions options = new()
  {
    Endpoint = new Uri("https://MYENDPOINT.azurewebsites.net")
  };

  HttpClientFactory factory = new(options);
}
```

The `HttpClientOptions` allows you to specify the following values:

* `Endpoint` - the URI to the backend datasync service.
* `HttpPipeline` - a list of delegating handlers to use for adjusting the HTTP request pipeline.
* `Timeout` - a `TimeSpan` (default: 60 seconds); specifies the `HttpClient.Timeout` value for created clients.
* `UserAgent` - the User-Agent header value for each request.  By default, a Datasync service specific value is used.

 The HTTP pipeline is an important mechanism by which you can adjust the requests as they flow through the Datasync Community Toolkit.  For instance, you might want to use a custom delegating handler for authentication, another for logging, and another for adding an API key to the request.  If you are adding a custom [HttpClientHandler](https://learn.microsoft.com/dotnet/api/system.net.http.httpclienthandler?view=net-8.0), then it should be the last element in the `HttpPipeline`.  You can specify a pipeline like this:

```csharp
using CommunityToolkit.Datasync.Client.Http;

public IHttpClientFactory GetClientFactory()
{
  HttpClientOptions options = new()
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

  HttpClientFactory factory = new(options);
}
```

The first element in the list becomes the root handler, then each successive handler is chained to the `InnerHandler` of the previous handler.

!!! tip
    You can easily set up basic and bearer authentication using the `GenericAuthenticationProvider`.  See the [authentication guide](./auth.md) for more details.

## Create a Datasync Service Client

Now that you have something to generate `HttpClient` objects, you can use it to create a `DatasyncServiceClient` for a specific service:

```csharp
var httpClient = GetClientFactory().GetClient();
var tableEndpoint = new Uri("/tables/movies", UriKind.Relative);
var moviesClient = new DatasyncServiceClient<Movie>(tableEndpoint, httpClient);
```

You can also specify the `JsonSerializerOptions` to use for serialization and deserialization of content from the service.

The `DatasyncServiceClient<TEntity>` uses two interfaces:

* `IReadOnlyDatasyncServiceClient<TEntity>` provides all the read operations (but no write operations).
* `IDatasyncServiceClient<TEntity>` provides both read and write operations.

The entity must provide a string-based `Id` and may provide the following additional fields that have special meaning:

* `UpdatedAt` - a `DateTimeOffset?` field.
* `Version` - a `string?` or `byte[]?` field.
* `Deleted` - a `bool` field.

When using online only access, these fields are optional.  The following property types are supported by the remote service:

* All primitive types (char, string, int, long, etc.)
* Date/Time types: `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`
* `Guid`
* Anything that can be transparently serialized to a string using the `JsonSerializerOptions` provided.

## Creating, modifying, or deleting an entity

Use the following:

* `AddAsync(entity, options)`
* `RemoveAsync(id, options)`
* `ReplaceAsync(entity, options)`

The options is a `DatasyncServiceOptions` object, which includes the following properties:

* `IncludeDeleted` - normally soft-deleted entities will not be affected.  Set this to `true` to affect soft-deleted entities.
* `ThrowIfMissing` - throws an EntityNotFoundException if the entity is missing (where appropriate).
* `Version` - if provided, the entity is not removed or replaced if the version provided does not match what the version of the entity on the server.

Convenience methods are also included:

* `AddAsync(entity)` - uses default settings.
* `RemoveAsync(entity)` - uses default settings, deleting only if the Version matches.
* `RemoveAsync(entity, force)` - uses default settings, deleting only if the Version matches or force is true.
* `ReplaceAsync(entity)` - uses default settings, replacing only if the Version matches.
* `ReplaceAsync(entity, force)` - uses default settings, replacing only if the Version matches or force is true.

Each method returns a `ServiceResponse` (remove operations) or `ServiceResponse<TEntity>` value.  For example:

```csharp
ServiceResponse<Movie> response = await client.AddAsync(newEntity, cancellationToken);
```

The ServiceResponse provides access to the underlying values of the `HttpResponseMessage`, but with a deserialized result.  This allows you to easily handle conflicts and update state.

```csharp
if (response.IsSuccessful && response.HasValue)
{
  // response.Value is a Movie that returns the added or replaced movie.
}

if (response.IsConflictStatusCode && response.HasValue) 
{
  // response.Value is the movie value from the service that caused the conflict 
}

if (!response.IsSuccessful && !response.IsConflictStatusCode && response.HasContent)
{
  // response.ContentStream holds the JSON error message for the status.
}
```

## Retrieving individual records

You can retrieve individual records from the server using `GetAsync()`:

```csharp
string id = "<id-of-entity-to-be-retrieved>";
ServiceResponse<Movie> response = await client.GetAsync(id);
if (response.IsSuccessful && response.HasValue)
{
  return response.Value;
}
```

As with the modification methods, you can handle errors using the `response.ContentStream`.

## Querying for data

The `DatasyncServiceClient` supports a subset of LINQ methods to handle query capabilities:

* `Where(predicate)` for filtering.
* `OrderBy(keySelector)`, `OrderByDescending(keySelector)`, `ThenBy(keySelector)`, and `ThenByDescending(keySelector)` for sorting.
* `Skip(count)` and `Take(count)` for paging.
* `Select(selector)` for selecting properties.

These can be used with a variety of terminating methods:

* `.CountAsync()` and `.LongCountAsync()` to count the matching entities.
* `.ToAsyncEnumerable()` to return an `IAsyncEnumerable<TEntity>`.
* `.ToAsyncPageable()` to return pages of entities at a time.
* `.ToArrayAsync()`, `.ToDictionaryAsync()`, `.ToHashSetAsync()`, and `.ToListAsync()` to return a collection asynchronously.
* `.ToObservableCollectionAsync()` to return an observable collection.

Using observable collections is common in UI focused applications.  For example, here is a snippet from an example view model:

```csharp
[ObservableProperty]
private string searchString;

[ObservableProperty]
private ConcurrentObservableCollection<Movie> searchResults = new();

[RelayCommand]
public async Task SearchCommandAsync(CancellationToken cancellationToken = default)
{
  try
  {
    _ = await service
      .Where(x => x.Title.Includes(searchString))
      .OrderBy(x => x.Title)
      .ThenByDescending(x => x.CreatedAt)
      .ToObservableCollectionAsync(searchResults, cancellationToken);
  }
  catch (Exception ex) 
  {
    // TODO: Handle errors
  }
}
```

### Filtering data

Filtering is done on the service when the `.Where()` method is before the terminating method, and on the client when the `.Where()` method is after the terminating method.  For example:

```csharp
var items = (await client.Where(x => !x.IsComplete))
  .ToListAsync())
  .Where(x => x.Title.StartsWith("The"));
```

The first `Where()` clause (return only incomplete items) is executed by the service, whereas the second `Where()` clause (starting with "The") is executed on the client.

The where clause supports operations that can be translated into an OData subset.  Operations include:

* Relational operators (`==`, `!=`, `<`, `<=`, `>`, `>=`),
* Arithmetic operators (`+`, `-`, `/`, `*`, `%`),
* Number precision (`Math.Floor`, `Math.Ceiling`),
* String functions (`Length`, `Substring`, `Replace`, `IndexOf`, `Equals`, `StartsWith`, `EndsWith`) (ordinal and invariant cultures only),
* Date properties (`Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`),
* Access properties of an object, and
* Expressions combining any of these operations.

!!! warning NoSQL data stores
    NoSQL data stores (such as MongoDb and Cosmos DB) generally limit the filtering operations that are available.  Most arithmatic, math, string, and property access filters are not available.

### Sorting data

Use `.OrderBy()`, `.OrderByDescending()`, `.ThenBy()`, and `.ThenByDescending()` with a property accessor to sort data.

``` csharp
var items = await remoteTable.OrderBy(x => x.IsComplete).ThenBy(x => x.Title).ToListAsync();
```

The sorting is done by the service.  You can't specify an expression in any sorting clause.  If you wish to sort by an expression, use client-side sorting:

``` csharp
var items = await remoteTable.ToListAsync().OrderBy(x => x.Title.ToLowerCase());
```

### Selecting properties

You can return a subset of data from the service:

``` csharp
var items = await remoteTable.Select(x => new { x.Id, x.Title, x.IsComplete }).ToListAsync();
```

### Return a page of data

You can return a subset of the data set using `.Skip()` and `.Take()` to implement paging:

``` csharp
var pageOfItems = await remoteTable.Skip(100).Take(10).ToListAsync();
```

You can also use an `IAsyncPageable<T>` to return pages at a time:

```csharp
IAsyncPageable<Movie> pageable = remoteTable.ToAsyncPageable();
await foreach (Page<Movie> pageOfMovies in pageable.AsPages())
{
  // Process each page of movies.
}
```

In a real world app, you can use queries similar to the preceding example with a pager control or comparable UI to navigate between pages.

All the functions described so far are additive, so we can keep chaining them. Each chained call affects more of the query. One more example:

```csharp
var query = todoTable
                .Where(todoItem => todoItem.Complete == false)
                .Select(todoItem => todoItem.Text)
                .Skip(3).
                .Take(3);
List<string> items = await query.ToListAsync();
```

### Conflict resolution and optimistic concurrency

Two or more clients can write changes to the same item at the same time. Without conflict detection, the last write would overwrite any previous updates. **Optimistic concurrency control** assumes that each transaction can commit and therefore doesn't use any resource locking.  Optimistic concurrency control verifies that no other transaction has modified the data before committing the data. If the data has been modified, the transaction is rolled back.

Azure Mobile Apps supports optimistic concurrency control by tracking changes to each item using the `version` system property column that is defined for each table in your Mobile App backend. Each time a record is updated, Mobile Apps sets the `version` property for that record to a new value. During each update request, the `version` property of the record included with the request is compared to the same property for the record on the server. If the version passed with the request doesn't match the backend, then the client library raises a `ConflictException<T>` exception. The type included with the exception is the record from the backend containing the servers version of the record. The application can then use this information to decide whether to execute the update request again with the correct `version` value from the backend to commit changes.

In addition to enabling optimistic concurrency, you must also catch the `ConflictException<T>` exception in your code.  Resolve the conflict by applying the correct `version` to the updated record and then repeat the call with the resolved record. The following code shows how to resolve a write conflict once detected:

```csharp
private async void UpdateToDoItem(TodoItem item)
{
    ConflictException<TodoItem> exception = null;

    try
    {
        //update at the remote table
        await remoteTable.ReplaceAsync(item);
    }
    catch (ConflictException<TodoItem> writeException)
    {
        exception = writeException;
    }

    if (exception != null)
    {
        // Conflict detected, the item has changed since the last query
        // Resolve the conflict between the local and server item
        await ResolveConflict(exception.ClientEntity, exception.ServerEntity);
    }
}


private async Task ResolveConflict(TodoItem localItem, TodoItem serverItem)
{
    //Ask user to choose the resolution between versions
    MessageDialog msgDialog = new MessageDialog(
        String.Format("Server Text: \"{0}\" \nLocal Text: \"{1}\"\n",
        serverItem.Text, localItem.Text),
        "CONFLICT DETECTED - Select a resolution:");

    UICommand localBtn = new UICommand("Commit Local Text");
    UICommand ServerBtn = new UICommand("Leave Server Text");
    msgDialog.Commands.Add(localBtn);
    msgDialog.Commands.Add(ServerBtn);

    localBtn.Invoked = async (IUICommand command) =>
    {
        // To resolve the conflict, update the version of the item being committed. Otherwise, you will keep
        // catching a MobileServicePreConditionFailedException.
        localItem.Version = serverItem.Version;

        // Updating recursively here just in case another change happened while the user was making a decision
        UpdateToDoItem(localItem);
    };

    ServerBtn.Invoked = async (IUICommand command) =>
    {
        RefreshTodoItems();
    };

    await msgDialog.ShowAsync();
}
```
