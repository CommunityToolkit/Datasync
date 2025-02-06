// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text;

namespace CommunityToolkit.Datasync.OpenApiService.Extensions;

[ExcludeFromCodeCoverage]
public static class SqliteExtensions
{
    public static void EnableSqliteExtensions(this DbContext context)
    {
        foreach (IEntityType table in context.Model.GetEntityTypes())
        {
            IEnumerable<IProperty> props = table.GetProperties()
                .Where(prop => prop.ClrType == typeof(byte[]) && prop.ValueGenerated == ValueGenerated.OnAddOrUpdate);
            foreach (IProperty prop in props)
            {
                context.InstallSqliteUpdateTrigger(table.GetTableName(), prop.Name, "UPDATE");
            }
        }
    }

    public static void EnableSqliteExtensions(this ModelBuilder builder)
    {
        IEnumerable<IMutableProperty> props = builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(prop => prop.ClrType == typeof(byte[]) && prop.ValueGenerated == ValueGenerated.OnAddOrUpdate);
        foreach (IMutableProperty prop in props)
        {
            prop.SetValueConverter(new SqliteTimestampConverter());
            prop.SetDefaultValueSql("STRFTIME('%Y-%m-%d %H:%M:%f', 'NOW')");
        }
    }

    public static void InstallSqliteUpdateTrigger(this DbContext context, string tableName, string fieldName, string operation)
    {
        string sql = $@"
            CREATE TRIGGER s_{tableName}_{fieldName}_{operation} AFTER {operation} ON {tableName}
            BEGIN
                UPDATE {tableName}
                SET {fieldName} = STRFTIME('%Y-%m-%d %H:%M:%f', 'NOW')
                WHERE rowid = NEW.rowid;
            END
        ";
        context.Database.ExecuteSqlRaw(sql);
    }
}

[ExcludeFromCodeCoverage]
public class SqliteTimestampConverter : ValueConverter<byte[], string>
{
    public SqliteTimestampConverter() : base(v => ToDb(v), v => FromDb(v))
    {
    }

    public static string ToDb(byte[] v) => Encoding.UTF8.GetString(v);
    public static byte[] FromDb(string v) => Encoding.UTF8.GetBytes(v);
}
