# TodoApp client for Blazor WebAssembly

!!! info "Blazor usage"
    While the Datasync Community Toolkit doesn't require any specific client-side technologies, offline usage requires access to a SQLite database.  Blazor WebAssembly runs in the browser sandbox and does not have this access, so this sample demonstrates **online-only** usage of the datasync client instead of the offline synchronization pattern used by the other TodoApp samples.  See [online operations](../../in-depth/client/online.md) for more information on the online-only client, and [Blazor WASM support](../../in-depth/client/advanced/blazor-wasm.md) for the current limitations of using Blazor WASM with the Datasync Community Toolkit.

## Run the application first

The Blazor WASM sample hosts an ASP.NET Core server (which also serves the compiled Blazor WebAssembly client) backed by an Entity Framework Core in-memory database, so there is nothing extra to configure to run it locally.

* Open `samples/todoapp-blazor-wasm/TodoApp.BlazorWasm.sln` in Visual Studio (or your editor of choice).
* Set `TodoApp.BlazorWasm.Server` as the startup project.
* Press F5 (or run `dotnet run` from the `TodoApp.BlazorWasm.Server` folder) to start the application.

The database is seeded with a few sample todo items the first time it starts, since it uses an in-memory database that is recreated on every restart.

## The code

### The server

The server is a standard ASP.NET Core host that serves the Blazor WebAssembly client's static files in addition to the datasync API.  The interesting parts of `Program.cs` are the same as any other datasync server:

    builder.Services.AddDbContext<TodoContext>(options => options.UseInMemoryDatabase("TodoAppDb"));
    builder.Services.AddDatasyncServices();
    builder.Services.AddControllersWithViews();

The `TodoContext` is a normal `DbContext` with a single `DbSet<TodoItem>`, and `TodoItem` inherits from `EntityTableData` in the usual way.

The `TodoItemsController` exposes the datasync API for `TodoItem`:

    [Route("tables/todoitems")]
    public class TodoItemsController : TableController<TodoItem>
    {
        public TodoItemsController(TodoContext context) : base()
        {
            Repository = new EntityTableRepository<TodoItem>(context);

            // UnsafeEntityLogging is intentionally left at its secure default (false).
            // This sample stores user-supplied TodoItem content (Title), so only the
            // entity ID is logged; the full serialized entity is never written to the logs.
            Options = new TableControllerOptions { UnsafeEntityLogging = false };
        }
    }

As with [the other sample server](./server.md), `UnsafeEntityLogging` is set explicitly to its secure default to show how you can control how much entity data is written to your logs.

### The client

Unlike the other TodoApp samples, the Blazor WebAssembly client doesn't use `OfflineDbContext` — it talks to the datasync service directly using `DatasyncServiceClient<T>` for each request, which is the recommended approach for [online-only client scenarios](../../in-depth/client/online.md).

The `DatasyncServiceClient<TodoItemDto>` is registered in `Program.cs`:

    builder.Services.AddHttpClient("DatasyncClient", client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    });

    builder.Services.AddScoped<DatasyncServiceClient<TodoItemDto>>(sp =>
    {
        IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        HttpClient httpClient = httpClientFactory.CreateClient("DatasyncClient");
        Uri tableEndpoint = new("/tables/todoitems", UriKind.Relative);
        return new DatasyncServiceClient<TodoItemDto>(tableEndpoint, httpClient);
    });

The `TodoService` class wraps the `DatasyncServiceClient<TodoItemDto>` with the CRUD operations the UI needs, using the LINQ query support and the `AddAsync()`/`ReplaceAsync()`/`RemoveAsync()` methods on the client directly (there is no local queue or push/pull step, since every operation talks to the server immediately):

    public async Task<IEnumerable<TodoItemDto>> GetTodoItemsAsync()
    {
        List<TodoItemDto> items = await todoClient.Where(item => !item.Deleted).ToListAsync();
        return items;
    }

    public async Task<TodoItemDto> CreateTodoItemAsync(string title)
    {
        TodoItemDto newItem = new() { Title = title };
        ServiceResponse<TodoItemDto> response = await todoClient.AddAsync(newItem);
        if (response.IsSuccessful && response.HasValue)
        {
            return response.Value!;
        }

        throw new InvalidOperationException($"Failed to create todo item: {response.ReasonPhrase}");
    }

`UpdateTodoItemAsync()` and `DeleteTodoItemAsync()` follow the same pattern using `ReplaceAsync()` and `RemoveAsync()` respectively.  Because there is no offline queue, any failure (including a conflict from `ReplaceAsync()`) is surfaced to the caller immediately via the `ServiceResponse<T>` rather than being resolved later during a push operation.
