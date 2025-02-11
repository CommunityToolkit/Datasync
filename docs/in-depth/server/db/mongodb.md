# MongoDb

Use the [CommunityToolkit.Datasync.Server.MongoDb](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.MongoDb) package to add support for this repository.  Underneath, the MongoDb repository uses `MongoDB.Driver` to access the store.

## Setup

Create a client in the normal way:

    MongoClientSettings clientSettings = MongoClientSettings.FromConnectionString(connectionString);
    MongoClient client = new MongoClient(clientSettings);

It's best to wrap this in a pooled or singleton service to avoid connection exhaustion.

Your entity should inherit from `MongoTableData`:

    public class MyEntity : MongoTableData
    {
      public string Title { get; set; }
    }

In your table controller:

    [Route("tables/[controller]")]
    public class MyEntityController : TableController<Entity>
    {
      public MyEntityController(MongoClient client)
      {
        IMongoDatabase database = client.GetDatabase("mydatabase");
        Repository = new MongoDBRepository<MyEntity>(database.GetCollection<MyEntity>("entities"));
      }
    }

## Known issues

The MongoDB implementation within Cosmos is split into vCore and RU format (also known as dedicated and serverless SKUs).  Do not use the "serverless" or RU SKU.  Ensure you are using Azure Cosmos DB for MongoDB (vCore). For more information, see [the Azure documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/mongodb/vcore/).
