# Custom repositories

This article is the third in a series of tutorial about the [Datasync Community Toolkit][toolkit], which is a set of open source libraries for building client-server applications where the application data is available offline. Thus far, I've [walked through creating a project](./part-1.md) and [introduced you to the standard repositories](./part-2.md). 

The standard repositories are excellent choices if you are starting afresh and they happen to meet your requirements. However, most enterprise projects don't start from scratch. When you are adding offline or mobile capabilities to an existing web application, for example, you already have a database schema in place. It may not to be suitable for datasync capabilities. Even if you happened to have suitable data within the schema to act as the metadata for datasync, it may be named differently, the wrong resolution, split among multiple tables, or not supported by Entity Framework Core. 

Whatever the reason, this is a great time to write a custom repository.

## Introducing a custom repository

Implementing a custom repository is not complex. You need a class that implements the `IRepository<TEntity>` interface (which is a CRUD interface combined with an `IQueryable<TEntity>` for searching) and you need to ensure that the results from the repository are free from side effects. 

As an example of this last point, entities from Entity Framework Core are "tracked" - they have a tracking link to the returned entity within the data structures that EF Core keeps around with the context. A change to the returned entity will mark the entity as "dirty" and a subsequent `SaveChangesAsync()` will save those changes to the database. Marking the entity as dirty would be considered a side effect.

The `IRepository<TEntity>` interface is defined like this:

```csharp
public interface IRepository<TEntity> where TEntity : ITableData
{
    ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default);
    ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default);
    ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default);
    ValueTask ReplaceAsync(TEntity entity, byte[]? version = null, CancellationToken cancellationToken = default);
}
```

The `ITableData` contains the metadata for each datasync entity - `Id`, `UpdatedAt`, `Version`, and `Deleted`.  You can also create an abstract class that implements `ITableData` for your repository.  This is useful when you are building a reusable (or generic) repository.

## An example: The auto-incrementing integer key

Let's look at an example. I have a typical web-based EF Core Todo app with no datasync capabilities at all. Here is my model:

```csharp
public class TodoItem
{
  [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
  [Required, StringLength(128, MinimumLength=1)]
  public string Text { get; set; } = string.Empty;
  public bool IsComplete { get; set; }
}
```

Comparing this model to the metadata requirements listed in `ITableData`:

* The `Id` property is an auto-incrementing integer.
* There is no `Version` property or anything that looks like a concurrency check.
* There is no `Deleted` property.
* This version has an `UpdatedAt` I can use. Review the [information on Date/Time computed columns](https://learn.microsoft.com/ef/core/modeling/generated-properties?tabs=data-annotations#datetime-value-generation) from the EF Core documentation.

When creating my SQL table, I created a trigger to automatically update the `UpdatedAt` property when the entity is saved.  My `DbContext` looks like this:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>()
            .ToTable(tb => tb.HasTrigger("trg_TodoItems_UpdatedAt"));
        modelBuilder.Entity<TodoItem>().Property(p => p.CreatedAt)
            .HasDefaultValueSql("getdate()");

        modelBuilder.Entity<TodoItem>().Property(p => p.UpdatedAt)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);
    }
}
```

This is a normal setup for a non-datasync SQL table in EF Core 7+ with a trigger. If it's not familiar ground, I'd recommend [researching EF Core](https://learn.microsoft.com/ef/core/get-started/overview/first-app).

## Updating the entity to support datasync

I need to store three additional pieces of information with the entity (The mobile versions of the `Id`, `Version`, and `Deleted` properties). I can either add the extra properties to my entity schema or I can store them in a separate "mobile metadata" table and use a join or view.  The latter technique is useful when I can't alter the entity table.  

For the purposes of this example, I'm going to update the entity. I'm not going to cover how to update the database schema. Use [a DACPAC](https://learn.microsoft.com/sql/relational-databases/data-tier-applications/data-tier-applications), SQL scripts or [migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations) as you prefer. My `TodoItem` now looks like this:

```csharp
public class TodoItem
{
  [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
  [Required, StringLength(128, MinimumLength=1)]
  public string Text { get; set; } = string.Empty;
  public bool IsComplete { get; set; }

  // Additional fields needed by the Datasync Community Toolkit
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public string MobileId { get; set; } = Guid.NewGuid().ToString("N");
  [Timestamp]
  public byte[] Version { get; set; } = [];
  public bool Deleted { get; set; } = false;
}
```

In addition to these changes, I need to ensure that the `UpdatedAt` and `MobileId` properties (along with any other properties that I used for searching) are indexed. This will be done when I update the database schema.  If using migrations, I can define the indices in the `OnModelCreating()` method:

```csharp
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
      modelBuilder.Entity<TodoItem>()
        .ToTable(tb => tb.HasTrigger("trg_TodoItems_UpdatedAt"));

      modelBuilder.Entity<TodoItem>().Property(p => p.CreatedAt)
        .HasDefaultValueSql("getdate()");

      modelBuilder.Entity<TodoItem>().Property(p => p.UpdatedAt)
        .ValueGeneratedOnAddOrUpdate()
        .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

      // Additions for the Datasync Community Toolkit
      modelBuilder.Entity<TodoItem>().Property(p => p.MobileId)
        .ValueGeneratedOnAdd()
        .HasValueGenerator(typeof(SequentialGuidValueGenerator));

      modelBuilder.Entity<TodoItem>().HasIndex(p => p.MobileId).IsUnique();
      modelBuilder.Entity<TodoItem>().HasIndex(p => p.UpdatedAt);
      modelBuilder.Entity<TodoItem>().HasIndex(p => p.Deleted);
  }
```

The `SequentialGuidValueGenerator` class generates sequential GUID values optimized for use with Microsoft SQL Server clustered keys or indexes, which improves performance.

!!! info Triggers with Entity Framework Core
    For more details on handling triggers, see [the Entity Framework Core docs](https://learn.microsoft.com/ef/core/what-is-new/ef-core-7.0/breaking-changes?tabs=v7#sqlserver-tables-with-triggers)

## Build a custom repository

Every repository has two parts:

* A DTO model that implements `ITableData`.  The standard repositories provide an abstract class to assist with this.
* A repository implementation that implements `IRepository<TEntity>`.

I'll create the DTO first:

```csharp
public class TodoItemDTO : ITableData
{
    public string Id { get; set; } = string.Empty;
    public bool Deleted { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];
    public string Text { get; set; } = string.Empty;
    public bool IsComplete { get; set; }

    public bool Equals(ITableData? other)
        => other is not null && Id == other.Id && Version.SequenceEqual(other.Version);
}
```

Note that I'm just implementing the `ITableData` interface here.  I could have also implemented this repository using an abstract class:

```csharp
public abstract class EntityDTO : ITableData
{
    public string Id { get; set; } = string.Empty;
    public bool Deleted { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];

    public virtual bool Equals(ITableData? other)
      => other is not null && Id == other.Id && Version.SequenceEqual(other.Version);
}

public class TodoItemDTO : EntityDTO
{
    public string Text { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}
```

The effect is the same.  Use whichever makes you more comfortable.  Err towards the latter option (using an abstract class) if you are creating a generic repository.

Next, create the repository implementation:

```csharp
public class TodoItemRepository(AppDbContext dbContext) : IRepository<TodoItemDTO>
{
    public static readonly Func<TodoItem, int, TodoItemDTO> ToDTO = (x, idx) => new TodoItemDTO
    {
        Id = x.MobileId,
        Deleted = x.Deleted,
        UpdatedAt = x.UpdatedAt,
        Version = [..x.Version],
        Text = x.Text,
        IsComplete = x.IsComplete
    };

    public ValueTask<IQueryable<TodoItemDTO>> AsQueryableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(dbContext.TodoItems.AsNoTracking().Select(ToDTO).AsQueryable());

    public async ValueTask CreateAsync(TodoItemDTO entity, CancellationToken cancellationToken = default)
    {
        TodoItem dbEntity = new()
        {
            // Don't set properties that are set by the database.
            CreatedAt = DateTimeOffset.UtcNow,
            Text = entity.Text,
            IsComplete = entity.IsComplete,
            // MobileId is **NOT** set by the database.
            MobileId = string.IsNullOrEmpty(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id
        };

        await WrapExceptionAsync(dbEntity.MobileId, async () =>
        {
            if (dbContext.TodoItems.Any(x => x.MobileId == dbEntity.MobileId))
            {
                throw new HttpException((int)HttpStatusCode.Conflict)
                {
                    Payload = await GetEntityAsync(dbEntity.MobileId, cancellationToken).ConfigureAwait(false)
                };
            }

            _ = dbContext.TodoItems.Add(dbEntity);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            CopyBack(dbEntity, entity);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrEmpty(id);
        await WrapExceptionAsync(id, async () =>
        {
            TodoItem storedEntity = await dbContext.TodoItems.SingleOrDefaultAsync(x => x.MobileId == id, cancellationToken).ConfigureAwait(false) 
                ?? throw new HttpException((int)HttpStatusCode.NotFound);
            if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
            {
                throw new HttpException((int)HttpStatusCode.PreconditionFailed)
                {
                    Payload = await GetEntityAsync(id, cancellationToken).ConfigureAwait(false)
                };
            }
            _ = dbContext.TodoItems.Remove(storedEntity);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TodoItemDTO> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrEmpty(id);
        TodoItem storedEntity = await dbContext.TodoItems.SingleOrDefaultAsync(x => x.MobileId == id, cancellationToken).ConfigureAwait(false)
            ?? throw new HttpException((int)HttpStatusCode.NotFound);
        return ToDTO(storedEntity, 0);
    }

    public async ValueTask ReplaceAsync(TodoItemDTO entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrEmpty(entity.Id);
        await WrapExceptionAsync(entity.Id, async () =>
        {
            TodoItem storedEntity = await dbContext.TodoItems.SingleOrDefaultAsync(x => x.MobileId == entity.Id, cancellationToken).ConfigureAwait(false)
                ?? throw new HttpException((int)HttpStatusCode.NotFound);
            if (version?.Length > 0 && !storedEntity.Version.SequenceEqual(version))
            {
                throw new HttpException((int)HttpStatusCode.PreconditionFailed) 
                { 
                    Payload = await GetEntityAsync(entity.Id, cancellationToken).ConfigureAwait(false) 
                };
            }

            storedEntity.Text = entity.Text;
            storedEntity.IsComplete = entity.IsComplete;
            storedEntity.Deleted = entity.Deleted;
            // TODO: If your stored entity does not update UpdatedAt/Version, then do it here.
            dbContext.Entry(storedEntity).CurrentValues.SetValues(entity);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            CopyBack(storedEntity, entity);
        }, cancellationToken).ConfigureAwait(false);
    }

    internal static void CopyBack(TodoItem dbEntity, TodoItemDTO entity)
    {
        entity.Id = dbEntity.MobileId;
        entity.UpdatedAt = dbEntity.UpdatedAt;
        entity.Version = [..dbEntity.Version];
        entity.Deleted = dbEntity.Deleted;
        // Add any other properties that could change during save here.
    }

    internal async Task WrapExceptionAsync(string id, Func<Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await action.Invoke().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new HttpException((int)HttpStatusCode.Conflict, ex.Message, ex) { Payload = await GetEntityAsync(id, cancellationToken).ConfigureAwait(false) };
        }
        catch (DbUpdateException ex)
        {
            throw new RepositoryException(ex.Message, ex);
        }
    }

    internal async Task<TodoItemDTO> GetEntityAsync(string id, CancellationToken cancellationToken = default)
    {
        TodoItem todoItem = await dbContext.TodoItems.SingleAsync(x => x.MobileId == id, cancellationToken);
        return ToDTO(todoItem, 0);
    }

    internal void ThrowIfNullOrEmpty(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException((int)HttpStatusCode.BadRequest, "ID is required");
        }
    }
}
```

Let's take you through the methods, starting with the private ones:

* `ThrowIfNullOrEmpty` is a basic checker to ensure the ID provided is set properly and throwing the right exception if not. Any `HttpException` is turned into the right HTTP response by the table controller for you.
* `GetEntityAsync` gets a disconnected version of the DTO. Since we are creating each DTO using a copy method, we don't need to worry about `.AsNoTracking()` in this repository.
* `WrapExceptionAsync` captures all the `DbUpdateException` exceptions and re-throws them as `RepositoryException` exceptions, which is required by the table controller to properly map the errors.
* `CopyBack` copies the data that might be changed when the database stores the model back to the DTO so that the incoming DTO is updated correctly.
* `ToDTO` (the func at the top of the method) turns a database model into a DTO by copying the data.

Then we have each of the methods required by the `IRepository<TEntity>` interface. Each of these methods does its designated function using Entity Framework Core, but transforming the incoming data and outgoing data into the DTO. For example, the `CreateAsync()` method (which is one of the more complex methods here) creates a new `TodoItem` based on the information in the incoming `TodoItemDTO`. It then uses the normal EF Core mechanism for adding an entity to the database. Finally, it updates the incoming DTO with the updated information from the database.

Despite this listing being long, it's still straight forward and readable. You can now use this repository in the same way as the standard repositories. Here is my controller, for example:

```csharp
[Route("tables/todoitems")]
public class TodoItemsController : TableController<TodoItemDTO>
{
    public TodoItemsController(AppDbContext context) : base()
    {
        Repository = new TodoItemRepository(context);
    }
}
```

Once your service is complete, run the server and interact with it via SwaggerUI, Postman, or your favorite REST UI. Here I am creating a record within the table with Postman:

![A screen shot of the Postman transaction](./img/2024-11-01-image1.png)

And here you can see a screen shot of the same record within the database (using the Visual Studio data browser):

![A screen shot of the SQL data](./img/2024-11-01-image2.png)

You will note that the `Id` and `CreatedAt` columns in my database model are not presented in the DTO, so they don't appear in the output from Postman.  However, they are maintained quite correctly by the database and/or repository.

## Best practices

Aside from the requirements of a repository, I've come up with a list of best practices:

* Don't try to include authorization, event handling, or other side effects in the repository. There are better ways to do those things. Limit your repository to just the database manipulations.
* Don't assume you need to write generic repositories. It's perfectly fine to have a repository per database entity, for example. The standard repositories are generic because they have to accomodate a wide range of models. You know your data already.
* Write tests for your repository and test it across all the corner cases. The unit tests for repositories in the [CommunityToolkit/Datasync][toolkit] GitHub repo are a great starting point.  Take a look at the `RepositoryTests.cs` particularly as that can be used as a basic test suite.

In short, the simpler the code is, the better time you will have - testing, debugging, and performance will all be improved.

## Final thoughts

A custom repository allows you to work with any data store where the data is in any shape, irrespective of how it is loaded or stored.

In addition to the four standard repositories, you can write repositories for whatever data store you want. Since you know how they work, you'll be able to share repositories between the datasync code and your own code with ease. I hope I've also demonstrated that you shouldn't be afraid of writing your own repository. They are isolated and easy to understand components that are easily tested and debugged.

In the [next tutorial](./part-4.md), I'm going to cover authorization; specifically how you can easily limit the data that is viewed by your users and update records prior to storage for authorization capabilities.

<!-- Links -->
[toolkit]: https://github.com/CommunityToolkit/Datasync