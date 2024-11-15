+++
title = "Database support"
weight = 15
+++

## Database Support

Entity Framework Core doesn't set up value generation for date/time columns.  (See [Date/time value generation](https://learn.microsoft.com/ef/core/modeling/generated-properties?tabs=data-annotations#datetime-value-generation)).  The default repository for Entity Framework Core automatically updates the `UpdatedAt` field for you.  However, if your database is updated outside of the repository, you  must arrange for the `UpdatedAt` and `Version` fields to be updated.

You can find sample implementations of each supported context in our [Database Tests](https://github.com/CommunityToolkit/Datasync/tree/main/tests/CommunityToolkit.Datasync.TestCommon/Databases).

For specific database support, see the following:

* [Azure Cosmos DB](./cosmos.md)
* [Azure SQL and SQL Server](./azuresql.md)
* [In Memory Datastore](./in-memory.md)
* [LiteDb](./litedb.md)
* [PostgreSQL](./postgresql.md)
* [Sqlite](./sqlite.md)