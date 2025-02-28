# Cosmos (SDK)

Support for Cosmos DB is available in two flavors:

* via [Entity Framework Core](./cosmos.md) - see [EF Core Azure Cosmos DB Provider](https://learn.microsoft.com/ef/core/providers/cosmos/)
* via the Cosmos DB SDK (this document) - see [the official documentation](https://learn.microsoft.com/azure/cosmos-db/nosql/quickstart-dotnet)

There are differences between the two providers that impact how you configure the container and the operations that are available.

Use the [CommunityToolkit.Datasync.Server.CosmosDb](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.CosmosDb) package to add support for this repository.

Azure Cosmos DB is a fully managed NoSQL database for high-performance applications of any size or scale.  See [Azure Cosmos DB for NoSQL .NET SDK](https://learn.microsoft.com/azure/cosmos-db/nosql/quickstart-dotnet) for information on using Azure Cosmos DB via the SDK.

## Set up

1.  Set up the Cosmos Container with a composite index that specifies the `UpdatedAt` and `Id` fields.  Composite indices can be added to a container through the Azure portal, ARM, Bicep, Terraform, or within code. Here's an example [bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview) resource definition that would store all datasync entities in the same container:

        resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
          parent: database
          name: containerName
          properties: {
            resource: {
              id: containerName
              partitionKey: {
                paths: [ '/entity' ]
                kind: 'Hash'
              }
              indexingPolicy: {
                indexingMode: 'consistent'
                includedPaths: [ { path: '/*' } ]
                excludedPaths: [ { path: '/_etag/?' } ]
                compositeIndexes: [
                  [
                    { path: '/updatedAt', order: 'ascending' }
                    { path: '/id', order: 'ascending' }
                  ]
                ]
              }
            }
          }
        }

    You must include a composite index for each combination of filters that you require.  A "client-side evaluation" exception will be generated (which is turned into a `400 Bad Request` by the server) if you do not have the appropriate composite indices for the request.
    
    You can also review the [bicep module](https://github.com/CommunityToolkit/Datasync/blob/main/samples/datasync-server-cosmosdb-singlecontainer/infra/resources.bicep) that is used in the sample.

    If you pull a subset of items in the table, ensure you specify all properties involved in the query.

2. Create an appropriate model that inherits from `CosmosTableData<TEntity>`:

        public class TodoItem : CosmosTableData<TodoItem>
        {
            [Required, MinLength(1)]
            public string Title { get; set; } = string.Empty;

            public bool IsComplete { get; set; }
        }

3. In `Program.cs`, configure a `CosmosClient` and add this to the services collection as a singleton:

        CosmosClient cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions()
            {
                UseSystemTextJsonSerializerWithOptions = new()
                {
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                        new DateTimeOffsetConverter(),
                        new DateTimeConverter(),
                        new TimeOnlyConverter(),
                        new SpatialGeoJsonConverter()
                    },
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                    ReferenceHandler = ReferenceHandler.Preserve
                }
            });

    You can obtain the connection string from the Cosmos resource page within the Azure Portal.

4. Create an `ICosmosTableOptions<TEntity>` object for each entity you wish to use for datasync purposes. The library provides two implementations:

    * `CosmosSharedTableOptions<TEntity>` stores entities in a single container using a partition key based on the entity name.
    * `CosmosSingleTableOptions<TEntity>` stores entities by compressing the ID and partition key into the ID of the entity.

    You can also specify your own implementation for custom logic in determining the partition key and ID.  Add the `ICosmosTableOptions<TEntity>` objects to the services collection in `Program.cs`:

        builder.Services.AddSingleton<ICosmosTableOptions<TodoItem>>(new CosmosSharedTableOptions<TodoItem>("TodoDb", "TodoContainer"));
        builder.Services.AddSingleton<ICosmosTableOptions<TodoList>>(new CosmosSharedTableOptions<TodoList>("TodoDb", "TodoContainer"));

5. Add the Cosmos repositories to the services collection within `Program.cs` with the following code:

        builder.Services.AddSingleton(typeof(IRepository<>), typeof(CosmosTableRepository<>));
        builder.Services.AddDatasyncServices();

6. Create a controller for each datasync entity:

        [Route("tables/[controller]")]
        public class TodoItemController : TableController<TodoItem>
        {
            public TodoItemController(IRepository<TodoItem> repository) : base(repository)
            {
            }
        }

You may add other settings to the repository to enable access control providers, logging, and other advanced features.  For more information, see the [documentation on the TableController](../index.md#table-controller-options).

## Avoid Client-side evaluations

When constructing a query within a client, avoid the following:

* Math operations such as division, multiplication, floor, ceiling, and round.
* Accessing date/time components such as year, day, or month.
* The use of DateOnly and TimeOnly types.

These are not supported by the query provider for Cosmos DB.  Using them will result in a client-side evaluation.  Client-side evaluations are not supported and will result in a `400 Bad Request` or `500 Internal Server Error`.

## Support and further information

Azure Cosmos DB is supported in the `Microsoft.AspNetCore.Datasync.CosmosDb` NuGet package since v9.0.1. For more information, review the following links:

* [Azure Cosmos DB .NET SDK](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/quickstart-dotnet) documentation.
* [Cosmos DB index policy](https://learn.microsoft.com/azure/cosmos-db/index-policy) documentation.
* [Cosmos DB single container reference sample](https://github.com/CommunityToolkit/Datasync/tree/main/samples/datasync-server-cosmosdb-singlecontainer)