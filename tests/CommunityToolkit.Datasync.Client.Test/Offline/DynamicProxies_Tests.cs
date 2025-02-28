// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

/// <summary>
/// Tests for: https://github.com/CommunityToolkit/Datasync/issues/217
/// See https://github.com/david1995/CommunityToolKit.Datasync-DynamicProxiesRepro/blob/main/ConsoleApp1/Program.cs
/// See https://github.com/CommunityToolkit/Datasync/issues/211
/// </summary>
[ExcludeFromCodeCoverage]
public class DynamicProxies_Tests : IDisposable
{
    private readonly string temporaryDbPath;
    private readonly string dataSource;
    private bool _disposedValue;

    public DynamicProxies_Tests()
    {
        this.temporaryDbPath = $"{Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())}.sqlite";
        this.dataSource = $"Data Source={this.temporaryDbPath};Foreign Keys=False";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this._disposedValue)
        {
            if (disposing)
            {
                // Really release the DB
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // If the file exists, it should be able to be deleted now.
                if (File.Exists(this.temporaryDbPath))
                {
                    File.Delete(this.temporaryDbPath);
                }
            }

            this._disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task OfflineDbContext_Queue_SupportsDynamicProxies()
    {
        SqliteConnection connection = new(this.dataSource);
        connection.Open();

        try
        {
            DbContextOptions<DynamicProxiesTestContext> dbContextOptions = new DbContextOptionsBuilder<DynamicProxiesTestContext>()
                .UseSqlite(connection)
                .UseLazyLoadingProxies()
                .Options;

            string key = Guid.CreateVersion7().ToString();
            await using (DynamicProxiesTestContext context = new(dbContextOptions))
            {
                context.Database.EnsureCreated();
                await context.DynamicProxiesEntities1.AddAsync(new DynamicProxiesEntity1
                {
                    Id = key,
                    Name = $"Test {DateTime.Now}",
                    LocalNotes = "These notes should not be serialized into DatasyncOperationsQueue",
                    RelatedEntity = new() { Id = Guid.NewGuid().ToString() }
                });
                await context.SaveChangesAsync();
            }

            await using (DynamicProxiesTestContext context = new(dbContextOptions))
            {
                DatasyncOperation operationAfterInsert = await context.DatasyncOperationsQueue.FirstAsync(o => o.ItemId == key);
                operationAfterInsert.EntityType.Should().EndWith("DynamicProxiesEntity1");
                operationAfterInsert.Version.Should().Be(0);

                // The LocalNotes should not be included
                operationAfterInsert.Item.Should().NotContain("\"localNotes\":");

                // Update the entity within the DbContext
                DynamicProxiesEntity1 entity = await context.DynamicProxiesEntities1.FirstAsync(e => e.Id == key);
                string updatedName = $"Updated name {DateTime.Now}";
                entity.Name = updatedName;
                await context.SaveChangesAsync();

                // There should be 1 operation.
                int operationsWithItemId = await context.DatasyncOperationsQueue.CountAsync(o => o.ItemId == key);
                operationsWithItemId.Should().Be(1);

                // Here is the operation after edit.
                DatasyncOperation operationAfterEdit = await context.DatasyncOperationsQueue.FirstAsync(o => o.ItemId == key);
                operationAfterEdit.EntityType.Should().EndWith("DynamicProxiesEntity1");
                operationAfterEdit.Version.Should().Be(1);
                operationAfterEdit.Item.Should().Contain($"\"name\":\"{updatedName}\"");

                // The LocalNotes should not be included
                operationAfterEdit.Item.Should().NotContain("\"localNotes\":");
            }
        }
        finally
        {
            connection.Close();
            connection.Dispose();
            SqliteConnection.ClearAllPools();
        }
    }
}

public class DynamicProxiesTestContext(DbContextOptions options) : OfflineDbContext(options)
{
    public virtual DbSet<DynamicProxiesEntity1> DynamicProxiesEntities1 { get; set; }

    public virtual DbSet<DynamicProxiesEntity2> DynamicProxiesEntities2 { get; set; }

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        optionsBuilder.Entity(typeof(DynamicProxiesEntity1), _ => { });
        optionsBuilder.Entity(typeof(DynamicProxiesEntity2), _ => { });
    }
}

public abstract class DatasyncBase
{
    [Key, StringLength(200)]
    public string Id { get; set; } = null!;

    public DateTimeOffset? UpdatedAt { get; set; }

    public string Version { get; set; }

    public bool Deleted { get; set; }
}

public class DynamicProxiesEntity1 : DatasyncBase
{
    [StringLength(255)]
    public string Name { get; set; }

    // this should not be synchronized
    [JsonIgnore]
    [StringLength(255)]
    public string LocalNotes { get; set; }

    [StringLength(200)]
    public string RelatedEntityId { get; set; }

    // this property should also not be serialized
    [JsonIgnore]
    public virtual DynamicProxiesEntity2 RelatedEntity { get; set; }
}

public class DynamicProxiesEntity2 : DatasyncBase;
