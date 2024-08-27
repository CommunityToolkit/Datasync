+++
title = "In-memory datastore"
+++

## In-memory data store

You can create an in-memory repository with no persistent storage by adding a singleton service for the repository in your `Program.cs`:

```csharp
IEnumerable<Model> seedData = GenerateSeedData();
builder.Services.AddSingleton<IRepository<Model>>(new InMemoryRepository<Model>(seedData));
```

Set up your table controller as follows:

```csharp
[Route("tables/[controller]")]
public class ModelController : TableController<Model>
{
    public MovieController(IRepository<Model> repository) : base(repository)
    {
    }
}
```
