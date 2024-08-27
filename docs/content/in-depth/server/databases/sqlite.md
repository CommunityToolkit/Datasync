+++
title = "Sqlite"
+++

## SqLite

> [!WARNING]
> Do not use SqLite for production services.  SqLite is only suitable for client-side usage in production.

SqLite doesn't have a date/time field that supports millisecond accuracy.  As such, it isn't suitable for anything except for testing.  If you wish to use SqLite, ensure you implement a value converter and value comparer on each model for date/time properties.  The easiest method to implement value converters and comparers is in the `OnModelCreating(ModelBuilder)` method of your `DbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    var timestampProps = builder.Model.GetEntityTypes().SelectMany(t => t.GetProperties())
        .Where(p => p.ClrType == typeof(byte[]) && p.ValueGenerated == ValueGenerated.OnAddOrUpdate);
    var converter = new ValueConverter<byte[], string>(
        v => Encoding.UTF8.GetString(v),
        v => Encoding.UTF8.GetBytes(v)
    );
    foreach (var property in timestampProps)
    {
        property.SetValueConverter(converter);
        property.SetDefaultValueSql("STRFTIME('%Y-%m-%d %H:%M:%f', 'NOW')");
    }
    base.OnModelCreating(builder);
}
```

Install an update trigger when you initialize the database:

```csharp
internal static void InstallUpdateTriggers(DbContext context)
{
    foreach (var table in context.Model.GetEntityTypes())
    {
        var props = table.GetProperties().Where(prop => prop.ClrType == typeof(byte[]) && prop.ValueGenerated == ValueGenerated.OnAddOrUpdate);
        foreach (var property in props)
        {
            var sql = $@"
                CREATE TRIGGER s_{table.GetTableName()}_{prop.Name}_UPDATE AFTER UPDATE ON {table.GetTableName()}
                BEGIN
                    UPDATE {table.GetTableName()}
                    SET {prop.Name} = STRFTIME('%Y-%m-%d %H:%M:%f', 'NOW')
                    WHERE rowid = NEW.rowid;
                END
            ";
            context.Database.ExecuteSqlRaw(sql);
        }
    }
}
```

Ensure that the `InstallUpdateTriggers` method is only called once during database initialization:

``` csharp
public void InitializeDatabase(DbContext context)
{
    bool created = context.Database.EnsureCreated();
    if (created && context.Database.IsSqlite())
    {
        InstallUpdateTriggers(context);
    }
    context.Database.SaveChanges();
}
```