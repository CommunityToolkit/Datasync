+++
title = "Sample Server"
+++

## A sample datasync server

Our sample datasync server provides an "Azure Developer CLI" ready implementation of the TodoItems service required by our TodoApp client samples.  It uses Azure SQL as its backend.

### Deploying the sample datasync server

1. Download the [sample server](https://github.com/CommunityToolkit/Datasync/tree/main/samples/datasync-server) from our GitHub repository.

1. Sign in to Azure using the Azure Developer CLI:

        azd auth login

   This will open up a browser.  If you encounter difficulties, refer to the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/) documentation.

2. Run the `azd up` command:
    
        azd up

   This will take approximately 5-15 minutes to deploy.  At the end of the process, you will be provided with the URL to your datasync service.  Record this URL for later use.

### Decommissioning the datasync server

You can easily decommission the datasync server using `azd down`.  This will remove all resources that were created by `azd up`.

## The code

Start with the `Program.cs` file:

    using CommunityToolkit.Datasync.Server;
    using Microsoft.EntityFrameworkCore;
    using Sample.Datasync.Server.Db;

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new ApplicationException("DefaultConnection is not set");

    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
    builder.Services.AddDatasyncServices();
    builder.Services.AddControllers();

    WebApplication app = builder.Build();

    // Initialize the database
    using (IServiceScope scope = app.Services.CreateScope())
    {
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.InitializeDatabaseAsync();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();

The lines that are highlighted are those added for the datasync service:

* Line 1 brings in the correct package.
* Line 11 adds datasync functionality to the services collection.

Next, the model:

    using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations;

    namespace Sample.Datasync.Server.Db;

    public class TodoItem : EntityTableData
    {
        [Required, MinLength(1)]
        public string Title { get; set; } = string.Empty;

        public bool IsComplete { get; set; }
    }

* Line 1, again, brings in support for the datasync repository; specifically the `EntityTableData` class.
* Line 6 ensures the model inherits from `EntityTableData` so that the correct properties are defined.

Our `DbContext` is simple:

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
        {

        }

        public DbSet<TodoItem> TodoItems => Set<TodoItem>();

        public async Task InitializeDatabaseAsync() 
        {
            await Database.EnsureCreatedAsync();
        }
    }

> [!TIP]
> You can (and should) use migrations or a SQL project instead of `EnsureCreatedAsync()` to create your database.

    [Route("tables/[controller]")]
    public class TodoItemController : TableController<TodoItem>
    {
        public TodoItemController(AppDbContext context) 
            : base(new EntityTableRepository<TodoItem>(context))
        {
        }
    }

In many respects, this datasync server is a standard ASP.NET Core Web API controller with minimal changes to support datasync services.

The sample has support for [NSwag](../../in-depth/server/openapi/nswag.md) and [Swashbuckle](../../in-depth/server/openapi/swashbuckle.md).  Set the following in the `appsettings.json` (or `appsettings.Development.json`) file:

    "Swagger": {
      "Driver": "nswag"
    }

The driver value can be `nswag` or `swashbuckle` based on your favored library.
