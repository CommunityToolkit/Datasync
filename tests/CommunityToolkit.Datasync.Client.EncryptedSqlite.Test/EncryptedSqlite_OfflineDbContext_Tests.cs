// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.EncryptedSqlite.Test.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite.Test;

[ExcludeFromCodeCoverage]
public class EncryptedSqlite_OfflineDbContext_Tests : EncryptedSqliteTestBase
{
    [Fact]
    public void OfflineDbContext_PersistsToEncryptedStore()
    {
        const string password = "Offl1ne-K3y!";
        string itemId;

        // Write using an OfflineDbContext backed by the encrypted store.
        {
            DbContextOptionsBuilder<OfflineTodoContext> builder = new();
            _ = builder.UseEncryptedSqlite(ConnectionString, password);
            using OfflineTodoContext context = new(builder.Options);
            _ = context.Database.EnsureCreated();

            TodoItem item = new() { Title = "offline" };
            itemId = item.Id;
            _ = context.TodoItems.Add(item);
            _ = context.SaveChanges();

            // Saving a synchronizable entity enqueues an operation in the offline operations queue.
            _ = context.DatasyncOperationsQueue.Count().Should().BeGreaterThan(0);
        }

        // The on-disk file must be encrypted.
        AssertFileIsEncrypted();

        // Re-open with the same key and read the entity back.
        {
            DbContextOptionsBuilder<OfflineTodoContext> builder = new();
            _ = builder.UseEncryptedSqlite(ConnectionString, password);
            using OfflineTodoContext context = new(builder.Options);

            TodoItem? reloaded = context.TodoItems.Find(itemId);
            _ = reloaded.Should().NotBeNull();
            _ = reloaded!.Title.Should().Be("offline");
        }
    }
}
