// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.DeltaTokenStore;
using CommunityToolkit.Datasync.Client.Offline.Models;
using CommunityToolkit.Datasync.Client.Offline.Operations;
using CommunityToolkit.Datasync.Client.Offline.OperationsQueue;
using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
///     A <see cref="DbContext"/> instance represents a session with the database and can be used to query and save
///     instances of your entities. An <see cref="OfflineDbContext"/> additionally supports synchronization operations
///     with a remote datasync service.  <see cref="DbContext"/> is a combination of the Unit Of Work and Repository patterns.
/// </summary>
/// <remarks>
///     <para>
///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more information
///         and examples.
///     </para>
///     <para>
///         Typically you create a class that derives from DbContext and contains <see cref="DbSet{TEntity}" />
///         properties for each entity in the model. If the <see cref="DbSet{TEntity}" /> properties have a public setter,
///         they are automatically initialized when the instance of the derived context is created.
///     </para>
///     <para>
///         Override the <see cref="OnConfiguring(DbContextOptionsBuilder)" /> method to configure the database (and
///         other options) to be used for the context. Alternatively, if you would rather perform configuration externally
///         instead of inline in your context, you can use <see cref="DbContextOptionsBuilder{TContext}" />
///         (or <see cref="DbContextOptionsBuilder" />) to externally create an instance of <see cref="DbContextOptions{TContext}" />
///         (or <see cref="DbContextOptions" />) and pass it to a base constructor of <see cref="DbContext" />.
///     </para>
///     <para>
///         The model is discovered by running a set of conventions over the entity classes found in the
///         <see cref="DbSet{TEntity}" /> properties on the derived context. To further configure the model that
///         is discovered by convention, you can override the <see cref="OnModelCreating(ModelBuilder)" /> method.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>,
///         <see href="https://aka.ms/efcore-docs-query">Querying data with EF Core</see>,
///         <see href="https://aka.ms/efcore-docs-change-tracking">Changing tracking</see>, and
///         <see href="https://aka.ms/efcore-docs-saving-data">Saving data with EF Core</see> for more information and examples.
///     </para>
/// </remarks>
public abstract partial class OfflineDbContext : DbContext
{
    /// <summary>
    /// A checker for the disposed state of this context.
    /// </summary>
    internal bool _disposedValue;
    internal DisposableLock _optionsLock = new();

    /// <summary>
    /// The operations queue.  This is used to store pending requests to the datasync service.
    /// </summary>
    [DoNotSynchronize]
    public DbSet<DatasyncOperation> DatasyncOperationsQueue => Set<DatasyncOperation>();

    /// <summary>
    /// The store for delta-tokens, which are used to keep track of the last synchronization time.
    /// </summary>
    [DoNotSynchronize]
    public DbSet<DatasyncDeltaToken> DatasyncDeltaTokens => Set<DatasyncDeltaToken>();

    /// <summary>
    /// The delta token store to use for pull operations.
    /// </summary>
    internal IDeltaTokenStore DeltaTokenStore { get; set; }

    /// <summary>
    /// The JSON Serializer Options to use in serializing and deserializing content.
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; } = DatasyncSerializer.JsonSerializerOptions;

    /// <summary>
    /// The offline options for the service.
    /// </summary>
    internal OfflineOptions? OfflineOptions { get; set; }

    /// <summary>
    /// The operations queue manager to use for push operations.
    /// </summary>
    internal OperationsQueueManager QueueManager { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OfflineDbContext" /> class. The 
    ///     <see cref="OnConfiguring(DbContextOptionsBuilder)" /> method will be called to 
    ///     configure the database (and other options) to be used for this context.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>
    ///     for more information and examples.
    /// </remarks>
    protected OfflineDbContext() : base()
    {
        QueueManager = new OperationsQueueManager(this);
        DeltaTokenStore = new DefaultDeltaTokenStore(this);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OfflineDbContext" /> class using the specified options.
    ///     The <see cref="OnConfiguring(DbContextOptionsBuilder)" /> method will still be called to allow further
    ///     configuration of the options.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see> and
    ///     <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see> for more information and examples.
    /// </remarks>
    /// <param name="options">The options for this context.</param>
    protected OfflineDbContext(DbContextOptions options) : base(options)
    {
        QueueManager = new OperationsQueueManager(this);
        DeltaTokenStore = new DefaultDeltaTokenStore(this);
    }

    /// <summary>
    /// Builds the offline options for a datasync operation.
    /// </summary>
    /// <returns>The offline options for the datasync operation.</returns>
    internal OfflineOptions BuildDatasyncOfflineOptions()
    {
        using IDisposable optionsLock = this._optionsLock.AcquireLock();
        if (OfflineOptions is null)
        {
            DatasyncOfflineOptionsBuilder builder = new(QueueManager.GetSynchronizableEntityTypes());
            OnDatasyncInitialization(builder);
            OfflineOptions = builder.Build();
        }
        
        return OfflineOptions;
    }

    /// <summary>
    ///     Override this method to set defaults and configure conventions before they run. This method is invoked before
    ///     <see cref="OnModelCreating" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a model is explicitly set on the options for this context then this method will not be run. However, it will
    ///         still run when creating a compiled model.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-pre-convention">Pre-convention model building in EF Core</see> for
    ///         more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="configurationBuilder">
    ///     The builder being used to set defaults and configure conventions that will be used to build the model for this context.
    /// </param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    /// <summary>
    ///     Override this method to configure the database (and other options) to be used for this context.
    ///     This method is called for each instance of the context that is created.
    ///     The base implementation does nothing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In situations where an instance of <see cref="DbContextOptions" /> may or may not have been passed
    ///         to the constructor, you can use <see cref="DbContextOptionsBuilder.IsConfigured" /> to determine if
    ///         the options have already been set, and skip some or all of the logic in
    ///         <see cref="OnConfiguring(DbContextOptionsBuilder)" />.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>
    ///         for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="optionsBuilder">
    ///     A builder used to create or modify options for this context. Databases (and other extensions)
    ///     typically define extension methods on this object that allow you to configure the context.
    /// </param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    ///     Override this method to further configure the model that was discovered by convention from the entity types
    ///     exposed in <see cref="DbSet{TEntity}" /> properties on your derived context. The resulting model may be cached
    ///     and re-used for subsequent instances of your derived context.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a model is explicitly set on the options for this context then this method will not be run. However, it
    ///         will still run when creating a compiled model. This may cause the <see cref="OfflineDbContext"/> entities
    ///         to not be configured properly.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see> for more 
    ///         information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="modelBuilder">
    ///     The builder being used to construct the model for this context. Databases (and other extensions) typically
    ///     define extension methods on this object that allow you to configure aspects of the model that are specific
    ///     to a given database.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    ///     Override this method to configure the datasync service connections for each entity.
    /// </summary>
    /// <param name="optionsBuilder">
    ///     The builder being used to construct the datasync service options for this context.
    /// </param>
    protected abstract void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder);

    /// <summary>
    ///     Pulls the changes from the remote service for the specified synchronizable entities.
    /// </summary>
    /// <param name="entityTypes">The list of entity types that should be pulled.</param>
    /// <param name="pullOptions">The options to use on this pull request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    public Task<PullResult> PullAsync(IEnumerable<Type> entityTypes, PullOptions pullOptions, CancellationToken cancellationToken  = default)
    {
        ArgumentNullException.ThrowIfNull(entityTypes, nameof(entityTypes));
        ArgumentValidationException.ThrowIfNotValid(pullOptions, nameof(pullOptions));

        OfflineOptions offlineOptions = BuildDatasyncOfflineOptions();
        IEnumerable<PullRequest> pullRequests = entityTypes.Select(type =>
        {
            EntityDatasyncOptions entityOptions = offlineOptions.GetOptions(type);
            return new PullRequest()
            {
                EntityType = type,
                QueryId = PullRequestBuilder.GetQueryIdFromQuery(type, entityOptions.QueryDescription),
                HttpClient = entityOptions.HttpClient,
                Endpoint = entityOptions.Endpoint,
                QueryDescription = entityOptions.QueryDescription
            };
        });
        return PullAsync(pullRequests, pullOptions, cancellationToken);
    }

    /// <summary>
    ///     Pulls the changes from the remote service for the specified synchronizable entities.
    /// </summary>
    /// <param name="configureAction">An action used to configure the pull request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    public Task<PullResult> PullAsync(Action<PullRequestBuilder> configureAction, CancellationToken cancellationToken = default)
    {
        PullRequestBuilder builder = new(QueueManager.GetSynchronizableEntityTypes(), BuildDatasyncOfflineOptions());
        configureAction.Invoke(builder);
        return PullAsync(builder.BuildPullRequests(), builder.BuildOptions(), cancellationToken);
    }

    /// <summary>
    ///     Pulls the changes from the remote service for the specified synchronizable entities.
    /// </summary>
    /// <param name="pullRequests">The information about the pull request.</param>
    /// <param name="pullOptions">The options to use on this pull request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    internal async Task<PullResult> PullAsync(IEnumerable<PullRequest> pullRequests, PullOptions pullOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pullRequests, nameof(pullRequests));
        ArgumentValidationException.ThrowIfNotValid(pullOptions, nameof(pullOptions));
        
        if (pullRequests.Count() == 0)
        {
            return new PullResult();
        }

        _ = await SaveChangesAsync(true, true, cancellationToken).ConfigureAwait(false);
        using IDisposable syncLock = await LockManager.AcquireSynchronizationLockAsync(cancellationToken).ConfigureAwait(false);

        foreach (PullRequest pullRequest in pullRequests)
        {
            int queueCount = await QueueManager.CountOperationsInQueueAsync(pullRequest.EntityType, cancellationToken).ConfigureAwait(false);
            if (queueCount > 0)
            {
                throw new DatasyncException($"There are still pending operations in queue for table '{pullRequest.EntityType.Name}'.");
            }
        }

        PullOperation operation = new(this, QueueManager.GetSynchronizableEntityTypes());
        PullResult result = await operation.ExecuteAsync(pullRequests, pullOptions, cancellationToken).ConfigureAwait(false);
        _ = await SaveChangesAsync(true, false, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    ///     Pushes the pending operations against the remote service for the provided set of entity types.
    /// </summary>
    /// <param name="entityTypes">The list of entity types in scope for this push operation.</param>
    /// <param name="pushOptions">The options for this push operation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the push operation.</returns>
    public virtual Task<PushResult> PushAsync(IEnumerable<Type> entityTypes, PushOptions pushOptions, CancellationToken cancellationToken = default)
        => QueueManager.PushAsync(entityTypes, pushOptions, cancellationToken);

    /// <summary>
    ///     Saves all changes made in this context to the database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method will automatically call <see cref="ChangeTracker.DetectChanges" />
    ///         to discover any changes to entity instances before saving to the underlying database. This can be disabled via
    ///         <see cref="ChangeTracker.AutoDetectChangesEnabled" />.
    ///     </para>
    ///     <para>
    ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
    ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
    ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
    ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more information
    ///         and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The number of state entries written to the database.
    /// </returns>
    /// <exception cref="DbUpdateException">
    ///     An error is encountered while saving to the database.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     A concurrency violation is encountered while saving to the database.
    ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
    ///     This is usually because the data in the database has been modified since it was loaded into memory.
    /// </exception>
    public override int SaveChanges()
        => SaveChanges(acceptAllChangesOnSuccess: true);

    /// <summary>
    ///     Saves all changes made in this context to the database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method will automatically call <see cref="ChangeTracker.DetectChanges" />
    ///         to discover any changes to entity instances before saving to the underlying database. This can be disabled via
    ///         <see cref="ChangeTracker.AutoDetectChangesEnabled" />.
    ///     </para>
    ///     <para>
    ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
    ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
    ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
    ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more information
    ///         and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="acceptAllChangesOnSuccess">
    ///     Indicates whether <see cref="ChangeTracker.AcceptAllChanges" /> is called after the changes have been sent successfully to the database.
    /// </param>
    /// <returns>
    ///     The number of state entries written to the database.
    /// </returns>
    /// <exception cref="DbUpdateException">
    ///     An error is encountered while saving to the database.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     A concurrency violation is encountered while saving to the database.
    ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
    ///     This is usually because the data in the database has been modified since it was loaded into memory.
    /// </exception>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => SaveChanges(acceptAllChangesOnSuccess, true);

    /// <summary>
    ///     Saves all changes made in this context to the database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method will automatically call <see cref="ChangeTracker.DetectChanges" />
    ///         to discover any changes to entity instances before saving to the underlying database. This can be disabled via
    ///         <see cref="ChangeTracker.AutoDetectChangesEnabled" />.
    ///     </para>
    ///     <para>
    ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
    ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
    ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
    ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more information
    ///         and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="acceptAllChangesOnSuccess">
    ///     Indicates whether <see cref="ChangeTracker.AcceptAllChanges" /> is called after the changes have been sent successfully to the database.
    /// </param>
    /// <param name="addToQueue">
    ///     Indicates whether synchronization operations should be added to the operations queue.
    /// </param>
    /// <returns>
    ///     The number of state entries written to the database.
    /// </returns>
    /// <exception cref="DbUpdateException">
    ///     An error is encountered while saving to the database.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     A concurrency violation is encountered while saving to the database.
    ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
    ///     This is usually because the data in the database has been modified since it was loaded into memory.
    /// </exception>
    public int SaveChanges(bool acceptAllChangesOnSuccess, bool addToQueue)
    {
        if (addToQueue)
        {
            QueueManager.UpdateOperationsQueue();
        }

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    ///     Saves all changes made in this context to the database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method will automatically call <see cref="ChangeTracker.DetectChanges" />
    ///         to discover any changes to entity instances before saving to the underlying database. This can be disabled via
    ///         <see cref="ChangeTracker.AutoDetectChangesEnabled" />.
    ///     </para>
    ///     <para>
    ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
    ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
    ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
    ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more
    ///         information and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous save operation. The task result contains the
    ///     number of state entries written to the database.
    /// </returns>
    /// <exception cref="DbUpdateException">
    ///     An error is encountered while saving to the database.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     A concurrency violation is encountered while saving to the database.
    ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
    ///     This is usually because the data in the database has been modified since it was loaded into memory.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    /// <summary>
    ///     Saves all changes made in this context to the database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method will automatically call <see cref="ChangeTracker.DetectChanges" />
    ///         to discover any changes to entity instances before saving to the underlying database. This can be disabled via
    ///         <see cref="ChangeTracker.AutoDetectChangesEnabled" />.
    ///     </para>
    ///     <para>
    ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
    ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
    ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
    ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more
    ///         information and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="acceptAllChangesOnSuccess">
    ///     Indicates whether <see cref="ChangeTracker.AcceptAllChanges" /> is called after
    ///     the changes have been sent successfully to the database.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous save operation. The task result contains the
    ///     number of state entries written to the database.
    /// </returns>
    /// <exception cref="DbUpdateException">
    ///     An error is encountered while saving to the database.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     A concurrency violation is encountered while saving to the database.
    ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
    ///     This is usually because the data in the database has been modified since it was loaded into memory.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess, addToQueue: true, cancellationToken);

    /// <summary>
    ///     Saves all changes made in this context to the database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method will automatically call <see cref="ChangeTracker.DetectChanges" />
    ///         to discover any changes to entity instances before saving to the underlying database. This can be disabled via
    ///         <see cref="ChangeTracker.AutoDetectChangesEnabled" />.
    ///     </para>
    ///     <para>
    ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
    ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
    ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
    ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more
    ///         information and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="acceptAllChangesOnSuccess">
    ///     Indicates whether <see cref="ChangeTracker.AcceptAllChanges" /> is called after
    ///     the changes have been sent successfully to the database.
    /// </param>
    /// <param name="addToQueue">
    ///     Indicates whether synchronization operations should be added to the operations queue.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous save operation. The task result contains the
    ///     number of state entries written to the database.
    /// </returns>
    /// <exception cref="DbUpdateException">
    ///     An error is encountered while saving to the database.
    /// </exception>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     A concurrency violation is encountered while saving to the database.
    ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
    ///     This is usually because the data in the database has been modified since it was loaded into memory.
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, bool addToQueue, CancellationToken cancellationToken = default)
    {
        if (addToQueue)
        {
            await QueueManager.UpdateOperationsQueueAsync(cancellationToken).ConfigureAwait(false);
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
    }

    #region IDisposable
    /// <summary>
    /// Ensure that the context has not been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the context has been disposed already.</exception>
    [DebuggerStepThrough]
    internal void CheckDisposed()
    {
        if (this._disposedValue)
        {
            throw new ObjectDisposedException(GetType().ShortDisplayName(), CoreStrings.ContextDisposed);
        }
    }

    /// <summary>
    ///     Releases the allocated resources for this context.
    /// </summary>
    /// <param name="disposing"></param>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>
    ///     for more information and examples.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (!this._disposedValue)
        {
            if (disposing)
            {
                this._optionsLock.Dispose();
                base.Dispose();
            }

            this._disposedValue = true;
        }
    }

    /// <summary>
    ///     Releases the allocated resources for this context.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>
    ///     for more information and examples.
    /// </remarks>
    public override void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
