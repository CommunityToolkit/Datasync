// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The <see cref="OfflineDbContext"/> class is a base class for a <see cref="DbContext"/> that is
/// used to store offline data that is later synchronized to a remote datasync service.
/// </summary>
public abstract class OfflineDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineDbContext"/> class.  The <see cref="OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will be called to configure the database (and other options) to be used for this context.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Standard entry point that is part of DbContext")]
    protected OfflineDbContext() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineDbContext"/> class. The <see cref="OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will still be called to configure the database to be used for this context.
    /// </summary>
    /// <param name="options">The options to use in configuring the <see cref="OfflineDbContext"/>.</param>
    protected OfflineDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// The list of entities that are awaiting synchronization.
    /// </summary>
    public DbSet<OfflineQueueEntity> DatasyncOperationsQueue => Set<OfflineQueueEntity>();

    /// <summary>
    /// The metadata storage for each table synchronization set.
    /// </summary>
    public DbSet<DatasyncTableInformation> DatasyncTableMetadata => Set<DatasyncTableInformation>();

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use for serializing and deserializing entities.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; }

    /// <summary>
    /// Used to manage the operations queue internally.
    /// </summary>
    internal IOperationsQueueManager OperationsQueueManager { get; set; }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        JsonSerializerOptions ??= new DatasyncServiceOptions().JsonSerializerOptions;
        OperationsQueueManager ??= new OperationsQueueManager(this);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OfflineQueueEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.EntityId);
        });

        modelBuilder.Entity<DatasyncTableInformation>(entity =>
        {
            entity.HasKey(e => e.QueryId);
            entity.Property(e => e.LastSynchronization).IsRequired().HasDefaultValue(0L);
        });

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
        List<EntityEntry> changedEntities = ChangeTracker.Entries().Where(t => t.State is EntityState.Added or EntityState.Deleted or EntityState.Modified).ToList();
        foreach (EntityEntry entry in changedEntities)
        {
            StoreChangeInOperationsQueue(entry);
        }
    }

    /// <summary>
    /// When <see cref="SaveChanges(bool)"/> or the async equivalent is called, this method is used to add the entry via
    /// the operations queue manager.  An <see cref="OfflineQueueEntity"/> is created for each change.
    /// </summary>
    /// <remarks>
    /// Every offline-capable entity must be derived from OfflineClientData and have a <see cref="DbSet{TEntity}"/> that
    /// is using the entity.
    /// </remarks>
    internal void StoreChangeInOperationsQueue(EntityEntry entry)
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
