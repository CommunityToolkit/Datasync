# Introducing the datasync client

The [Datasync Community Toolkit][1] is a set of open-source libraries for building client-server application where the application data is available offline.  Unlike, for example, [Google Firebase][2] or [AWS AppSync][3] (which are two competitors in this space), the [Datasync Community Toolkit][1] allows you to connect to any database, use any authentication, and provides robust authorization rules.  You can also run the service anywhere - on your local machine, in a container, or on any cloud provider.  Each side of the application (client and server) is implemented using .NET - [ASP.NET Core Web APIs][4] on the server side, and any .NET client technology (including [Avalonia][5], [MAUI][6], [Uno Platform][7], [WinUI3][8], and [WPF][9]) on the client side.

If you haven't already gone through the tutorial series for the server-side, I recommend you do that first. This tutorial series assumes you can easily set up a server to work with. [Read the server tutorial series now](../server/part-1.md).

You may remember from the server tutorial that the service side of the Datasync Community Toolkit is a RESTful web API that implements standard CRUD (Create-Read-Update-Delete) operations against a repository, along with an OData v4 query capability.  You don't need a special library.  In fact, we have a [todomvc sample][todomvc] that does nothing more than regular HTTP calls to manage the data.

During this tutorial series, we will be building a simple todo list application using Windows Presentation Framework (WPF).  You can find the base tutorial code [in our samples directory][todoapp]. All the operations that require access to data within the application pass through an implementation of the `ITodoService`.  As distributed, the application uses `InMemoryTodoService` - an implementation of `ITodoService` that uses a concurrent dictionary to store the data.  There are four methods to implement:

* Get all items
* Get a single item by ID
* Add a new item
* Replace an existing item

The `TodoItem` model is set up so that it contains the same metadata that the server-side model requires:

* `Id` is a globally unique string.
* `UpdatedAt` is a `DateTimeOffset` that records when the entity was last changed.
* `Version` is an opaque string that changes whenever the entity changes.

These properties are implemented in the `OfflineClientEntity` abstract class.  You can use this in your own projects to ensure your datasync models have the right metadata.  The UI and service are wired using dependency injection provided by the [CommunityToolkit.MVVM] project.

It's worthwhile taking some time to study the application prior to adding any new code to it. You can also consider using Avalonia, MAUI, WinUI3, or the Uno Platform for your application.  The Datasync Community Toolkit is based on .NET 8+ and does not have any client framework-specific code in it.

!!! info "Blazor usage"
    While the Datasync Community Toolkit doesn't require any specific client-side technologies, offline usage requires access to a SQLite database.  Blazor is limited to online usage only.

## Online operations

The core of the Datasync Community Toolkit is the synchronization of structured data from a server-side database to a client database.  However, before I talk about that topic, you should understand how you can interact with the service for online operations.  You can (as mentioned earlier) just use HTTP to communicate with the server.  The Datasync Community Toolkit client comes with a HTTP client that simplifies interacting with the server by providing:

1. A robust pipeline-based HTTP client to make it easier to use authentication and authorization.
2. A set of convenience methods for interacting with the server-side repository.
3. A set of LINQ methods to make searching for data online easier.

This functionality is integrated into the [CommunityToolkit.Datasync.Client] NuGet package.

Let's start by creating a new `ITodoService` implementation:

    public class OnlineTodoService : ITodoService
    {
      private const string baseUrl = "<insert-the-base-url-of-your-service-here>";
      private readonly DatsyncServiceClient<TodoItem> client;

      public OnlineTodoService()
      {
        // Initialize your client here.
      }

      public async Task<TodoItem> AddTodoItemAsync(string title, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      public async Task<List<TodoItem>> GetAllTodoItemsAsync(CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();

      }

      public async Task<TodoItem?> GetTodoItemAsync(string id, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      public async Task<TodoItem> ReplaceTodoItemAsync(TodoItem updatedItem, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }
    }

We will fill the details of this class in as we go through this tutorial.  Before I move on, you'll want to wire in the new class to your dependency injection service collection.  This is done in `App.xaml.cs`:

    Services = new ServiceCollection()
      .AddSingleton<ITodoService, OnlineTodoService>()
      .AddTransient<TodoListViewModel>()
      .AddScoped<IAlertService, AlertService>()
      .AddScoped<IAppInitializer, AppInitializer>()
      .BuildServiceProvider();

### Connecting to the service

Let's start with a basic connection profile.  This is placed inside the constructor of `OnlineTodoService`:

    public OnlineTodoService()
    {
      var clientOptions = new HttpClientOptions()
      {
        Endpoint = new Uri(baseUrl)
      };
      client = new DatasyncServiceClient<TodoItem>(clientOptions);
    }

Ultimately, the `DatasyncServiceClient<TEntity>` class communicates with the service via the standard `HttpClient` that has a `BaseAddress` property set to the root of the server.  You can just create a client like this:

    Uri tableControllerUri = new Uri("https://myserver/tables/todoitem");
    var client = new DatasyncServiceClient<TodoItem>(tableControllerUri, new HttpClient());

However, the client library also includes a full set of options for you to set.  For example, let's say you wanted to re-use the `HttpClient` for multiple table connections that are co-located on the server:

    HttpClient httpClient = new HttpClient() { BaseAddress = new Uri("https://myserver/") };
    Uri relativeUri = new Uri("/tables/todoitem", UriKind.RelativeUri);
    var client = new DatasyncServiceClient<TodoItem>(relativeUri, httpClient);

!!! warning "Beware the trailing slash"
    Note that the Uri provided for the `BaseAddress` has a trailing slash.  This is super-important!  The absolute URI is created by combining the BaseAddress with the provided relativeUri.  Most problems seen here are because of the semantics of combining a relative URI with the base address.

It's normal to produce an `IHttpClientFactory` for creating clients.  The client library has an implementation that is based on the `HttpClientOptions`.  Let's look at a "maximum options" version.  First, when defining your services collection, you can set up the HTTP client factory:

    HttpClientOptions options = new()
    {
      Endpoint = new Uri("https://myserver/"),
      HttpPipeline = [
        new LoggingHandler(),
        new AuthenticationHandler()
      ],
      Timeout = TimeSpan.FromSeconds(120),
      UserAgent = "Enterprise/Datasync-myserver-service"
    };

    Services = new ServiceCollection()
      .AddSingleton<HttpClientOptions>(options)
      .AddSingleton<IHttpClientFactory, HttpClientFactory>
      . /* the rest of your services here */
      .BuildServiceProvider();

If you are using Blazor, you can [set up the client factory using the normal mechanism](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests).  Now that you have a mechanism for injecting the `IHttpClientFactory`, you can use this in your constructor:

    public OnlineTodoService(IHttpClientFactory clientFactory)
    {
      var uri = new Uri("/tables/todoitem", UriKind.Relative);
      client = new DatasyncServiceClient<TodoItem>(uri, clientFactory.CreateClient());
    }

We'll get into some of the options within the service client in a later part of the tutorial.

## Retrieving, Adding, and Updating entities

Before I move to the query functionality, I'm going to look at the three methods that work with single entities.  Each of these operations (along with the delete entity operation, not represented in this application) looks pretty much the same.  I simplify the code with an extension method:

    public static class ServiceExtensions
    {
      public TEntity ReturnOrThrow<TEntity>(this ServiceResponse<TEntity> response)
        => response.IsSuccessful && response.HasValue 
        ? response.Value!
        : throw new ApplicationException(response.ReasonPhrase);
    }

This makes the three methods simple to understand:

    public async Task<TodoItem> AddTodoItemAsync(string title, CancellationToken cancellationToken = default)
      => (await client.AddAsync(new TodoItem { Title = title }, cancellationToken)).ReturnOrThrow();

    public async Task<TodoItem?> GetTodoItemAsync(string id, CancellationToken cancellationToken = default)
      => (await client.GetAsync(id, cancellationToken)).ReturnOrThrow();

    public async Task<TodoItem> ReplaceTodoItemAsync(TodoItem updatedItem, CancellationToken cancellationToken = default)
      => (await client.ReplaceAsync(updatedItem, cancellationToken)).ReturnOrThrow();

Each of the single-entity operations returns a `ServiceResponse<T>`.  This contains the majority of the `HttpResponseMessage` allowing you to react just as you normally would with a HTTP response.  In addition, the content is decoded and deserialized for you.  You have access to the raw content (using `.HasContent` and `.Content`) and the deserialized value (using `.HasValue` and `.Value`). To remove an entity, use `client.RemoveAsync()`, which returns a `ServiceResponse` (since a deletion does not return any data).

You can also provide more information:

* `ServiceResponse<TEntity> response = await client.AddAsync(clientSideItem, options);`
* `ServiceResponse response = await client.RemoveAsync(clientSideItem, options);`
* `ServiceResponse<TEntity> response = await client.ReplaceAsync(clientSideItem, options);`

Each operation can take a `DatasyncServiceOptions` object with the following properties:

* `ThrowIfMissing=true` will throw an exception if you ask to remove or replace an entity that does not exist.  If false, then you should check the `ServiceResponse` that is returned to determin success or failure.  This is useful when you are removing an entity and don't really care if the entity is missing or not.
* `IncludeDeleted=false`; allows you to "undelete" a soft-deleted entity by replacing the service side version.
* `Version` is set to the version of the entity (the default).  If not set, the operation is forced.

## Querying for data

The final method in our sample implementation is the query interface.  The `DatasyncServiceClient<TEntity>` class implements `TableQuery` - a subset of LINQ that is supported by our implementation of OData.  When the query is transmitted to the remote service, it is turned into an OData query string.  Full details are provided in the [in-depth documentation](https://communitytoolkit.github.io/Datasync/in-depth/client/online-operations/index.html#querying-for-data).  To query for all data:

    public async Task<List<TodoItem>> GetAllTodoItemsAsync(CancellationToken cancellationToken = default)
      => await client.ToListAsync(cancellationToken);

However, you can also use `Where`, `OrderBy`, `Skip`, and `Take` as needed.  

Let's take a look at some examples that aren't just "grab everything":

* Your table is set up for "soft-delete" and you want to download the deleted items so you can represent them with a line through them:

        var items = await client.IncludeDeletedItems().ToListAsync(cancellationToken);

* You have a `ListId` property and want to display items in a given list:

        var items = await client.Where(x => x.ListId == myListId).ToListAsync(cancellationToken);

* You want the items ordered by the `CreatedAt` property.

        var items = await client.OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);

## Wrapping up

Why start with online operations if this is an offline-first datasync library?  Figuring out all the options that can be used to communicate with the server is important.  When things go wrong (and they will), you need to understand what is going on.  Switching to an online view (and adding logging) is a great way to get started.

Secondly, many applications require both online and offline capabilities.  Let's say I have a CRM with customers, orders, and products.  I might deal with a select set of customers that I want to use offline, but I don't need to see the historical order data, nor do I need to see all the customers that have ever ordered anything from me.  I can search for customers online and mark a customer as being "important to me" so that it is included in the offline dataset.  I gain flexibility in creating a client application when I can operate both online and offline.

In the [next tutorial](./part-2.md), I'm going to talk about authenticating clients with the server.

<!-- links -->
[1]: https://github.com/CommunityToolkit/Datasync
[2]: https://firebase.google.com/
[3]: https://docs.aws.amazon.com/appsync/latest/devguide/what-is-appsync.html
[4]: https://learn.microsoft.com/training/modules/build-web-api-aspnet-core/
[5]: https://avaloniaui.net/
[6]: https://dotnet.microsoft.com/apps/maui
[7]: https://platform.uno/
[8]: https://learn.microsoft.com/windows/apps/winui/winui3/
[9]: https://wpf-tutorial.com/
[todomvc]: https://github.com/CommunityToolkit/Datasync/tree/main/samples/todoapp-mvc
[todoapp]: https://github.com/CommunityToolkit/Datasync/tree/main/samples/todoapp-tutorial
[CommunityToolkit.MVVM]: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
[CommunityToolkit.Datasync.Client]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Client#readme-body-tab