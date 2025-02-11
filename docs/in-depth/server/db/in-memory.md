# In-memory data store

The in-memory store uses an internal dictionary to store the entities.  This is useful for testing and for static data that is only refreshed when the server is updated.

## Set up

You can create an in-memory repository with no persistent storage by adding a singleton service for the repository in your `Program.cs`:

    IEnumerable<Model> seedData = GenerateSeedData();
    builder.Services.AddSingleton<IRepository<Model>>(new InMemoryRepository<Model>(seedData));

Set up your table controller as follows:

    [Route("tables/[controller]")]
    public class ModelController : TableController<Model>
    {
        public MovieController(IRepository<Model> repository) : base(repository)
        {
        }
    }
