// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The <see cref="OfflineDbContext"/> class is a base class for a <see cref="DbContext"/> that is
/// used to store offline data that is later synchronized to a remote datasync service.
/// </summary>
public abstract class OfflineDbContext : DbContext
{
    /// <summary>
    /// The list of entities that are awaiting synchronization.
    /// </summary>
    public DbSet<OfflineQueueEntity> DatasyncOperationsQueue => Set<OfflineQueueEntity>();

    /// <summary>
    /// The metadata storage for each table synchronization set.
    /// </summary>
    public DbSet<DatasyncTableInformation> DatasyncTableMetadata => Set<DatasyncTableInformation>();

    /// <summary>
    /// The set of options used to configure the offline dataset.  This is normally set
    /// by overriding the <see cref="OnConfiguring(DbContextOptionsBuilder)"/> method and
    /// setting it before calling the base method.
    /// </summary>
    public IOfflineDatasetOptions OfflineDatasetOptions { get; protected set; }

    /// <summary>
    /// Used to manage the operations queue internally.
    /// </summary>
    internal IOperationsQueueManager OperationsQueueManager { get; set; }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        OfflineDatasetOptions ??= new OfflineDatasetOptions();
        OperationsQueueManager ??= new OperationsQueueManager(this, OfflineDatasetOptions);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Add the model setup for the offline operations queue and metadata tables.
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    public virtual new int SaveChanges() => SaveChanges(true);

    /// <inheritdoc />
    public virtual new int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StoreChangesInOperationsQueue();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public virtual new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(true, cancellationToken);

    /// <inheritdoc />
    public virtual new Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StoreChangesInOperationsQueue();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// When <see cref="SaveChanges(bool)"/> or the async equivalent is called, this method looks through the changes
    /// to see if any should be pushed to the service.  An <see cref="OfflineQueueEntity"/> is created for each change.
    /// </summary>
    /// <remarks>
    /// Every offline-capable entity must be derived from OfflineClientData and have a <see cref="DbSet{TEntity}"/> that
    /// is using the entity.
    /// </remarks>
    internal void StoreChangesInOperationsQueue()
    {
        ChangeTracker.DetectChanges();
        foreach (EntityEntry entry in ChangeTracker.Entries().Where(t => t.State is EntityState.Added or EntityState.Deleted or EntityState.Modified))
        {
            if (entry.Entity is OfflineClientEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        OperationsQueueManager.AddCreateOperation(entry);
                        break;
                    case EntityState.Deleted:
                        OperationsQueueManager.AddDeleteOperation(entry);
                        break;
                    case EntityState.Modified:
                        OperationsQueueManager.AddUpdateOperation(entry);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown entity state");
                }
            }
        }
    }
}
