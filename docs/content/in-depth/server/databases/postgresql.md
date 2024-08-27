+++
title = "PostgreSQL"
+++

## PostgreSQL

Create a trigger for each entity:

```sql
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
```

You can install this trigger using either a migration or immediately after `EnsureCreated()` to create the database.

In the `OnModelCreating()` method of your context, add the following for each entity:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
  // Repeat the next line for each synchronizable entity
  modelBuilder.Entity<Model>().Property(m => m.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
  base.OnModelCreating(modelBuilder);
}
```

* [Test PostgreSQL Context](https://github.com/CommunityToolkit/Datasync/blob/main/tests/CommunityToolkit.Datasync.TestCommon/Databases/Postgresql/PgDbContext.cs)
