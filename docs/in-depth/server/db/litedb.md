# LiteDB

[LiteDB](https://www.litedb.org/) is a serverless database delivered in a single small DLL written in .NET C# managed code.  It's a simple and fast NoSQL database solution for stand-alone applications.  To use LiteDb with on-disk persistent storage:

1. Install the `Microsoft.AspNetCore.Datasync.LiteDb` package from NuGet.

2. Add a singleton for the `LiteDatabase` to the `Program.cs`:

        const connectionString = builder.Configuration.GetValue<string>("LiteDb:ConnectionString");
        builder.Services.AddSingleton<LiteDatabase>(new LiteDatabase(connectionString));

3. Derive models from the `LiteDbTableData`:

        public class TodoItem : LiteDbTableData
        {
            public string Title { get; set; }
            public bool Completed { get; set; }
        }

    You can use any of the `BsonMapper` attributes that are supplied with the LiteDb NuGet package.

4. Create a controller using the `LiteDbRepository`:

        [Route("tables/[controller]")]
        public class TodoItemController : TableController<TodoItem>
        {
            public TodoItemController(LiteDatabase db) : base()
            {
                Repository = new LiteDbRepository<TodoItem>(db, "todoitems");
            }
        }
