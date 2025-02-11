# PostgreSQL

PostgreSQL is configured via Entity Framework Core.  Use the [CommunityToolkit.Datasync.Server.EntityFrameworkCore](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.EntityFrameworkCore) package to add support for this repository.

## Set up

Create a trigger for each entity:

    CREATE OR REPLACE FUNCTION entity_datasync() RETURNS trigger AS $$
    BEGIN
        NEW."UpdatedAt" = NOW() AT TIME ZONE 'UTC';
        NEW."Version" = convert_to(gen_random_uuid()::text, 'UTF8');
        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;

    CREATE OR REPLACE TRIGGER
        entity_datasync
    BEFORE INSERT OR UPDATE ON
        "entity"
    FOR EACH ROW EXECUTE PROCEDURE
        entity_datasync();

You can install this trigger using either a migration or immediately after `EnsureCreated()` to create the database.

In the `OnModelCreating()` method of your context, add the following for each entity:

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      // Repeat the next line for each synchronizable entity
      modelBuilder.Entity<Model>().Property(m => m.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
      base.OnModelCreating(modelBuilder);
    }

## Support and further information

* [Official documentation](https://www.npgsql.org/efcore/?tabs=onconfiguring)
* [Test PostgreSQL Context](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestCommon/Databases/Postgresql/PgDbContext.cs)
