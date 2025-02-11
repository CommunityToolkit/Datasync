# MySQL

MySQL is supported via the Entity Framework Core repository. Add the [`Pomelo.EntityFrameworkCore.Mysql`](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql) driver to your project.

> [!INFO]
> You can probably use the `MySql.EntityFrameworkCore` library as well.  However, we only test with the Pomelo driver.

## Setup

In the `OnModelCreating()` method of your context, add the following for each entity:

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Model>().Property(m => m.UpdatedAt)
        .ValueGeneratedOnAddOrUpdate();

      modelBuilder.Entity<Model>().Property(m => m.Version)
        .IsRowVersion();

      base.OnModelCreating(modelBuilder);
    }

* [Test MySQL Context](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestCommon/Databases/MySQL/MysqlDbContext.cs)
