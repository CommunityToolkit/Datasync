# MySQL

MySQL is supported via the Entity Framework Core repository. Add the [`Pomelo.EntityFrameworkCore.Mysql`](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql) driver to your project.

!!! note
    As of the v10.0.0 of the Datasync Library, we no longer test MySQL.  It **should** work, but we can not make any guarantees.  Open an issue if you find problems.

## Set up

In the `OnModelCreating()` method of your context, add the following for each entity:

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Model>().Property(m => m.UpdatedAt)
        .ValueGeneratedOnAddOrUpdate();

      modelBuilder.Entity<Model>().Property(m => m.Version)
        .IsRowVersion();

      base.OnModelCreating(modelBuilder);
    }


## Support and further information

* [Official docs: MySql and Entity Framework Core](https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core.html)
* [Pomelo docs: MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
* [Test MySQL Context](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestCommon/Databases/MySQL/MysqlDbContext.cs)
