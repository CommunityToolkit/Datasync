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

You can install this trigger using either a migration or immediately after `EnsureCreated()` to create the database.

* [Test Azure SQL Context](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestCommon/Databases/AzureSql/AzureSqlDbContext.cs)