+++
title = "Azure SQL & SQL Server"
+++

## Azure SQL and SQL Server

Create a trigger for each entity:

```sql
CREATE OR ALTER TRIGGER [dbo].[TodoItems_UpdatedAt] ON [dbo].[TodoItems]
    AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE 
        [dbo].[TodoItems] 
    SET 
        [UpdatedAt] = GETUTCDATE() 
    WHERE 
        [Id] IN (SELECT [Id] FROM INSERTED);
END
```

You can install this trigger using either a migration or immediately after `EnsureCreated()` to create the database.  As with any Entity Framework Core based system, you must adjust your model within your `DbContext` as follows:

```csharp
protected void OnModelCreating(ModelBuilder builder)
{
  base.OnModelCreating(builder);

  builder.Entity<TodoItem>().ToTable(t => t.HasTrigger("TodoItems_UpdatedAt"));
}
```

For more information, review the [Entity Framework Core release notes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-7.0/breaking-changes?tabs=v7#sqlserver-tables-with-triggers).

* [Test Azure SQL Context](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestCommon/Databases/AzureSql/AzureSqlDbContext.cs)