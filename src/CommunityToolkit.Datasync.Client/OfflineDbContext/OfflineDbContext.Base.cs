// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Context;
using CommunityToolkit.Datasync.Client.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An <see cref="OfflineDbContext"/> instance represents a combination of the Unit of Work
/// and Repository patterns such that it can be used to query a local database, just like a
/// <see cref="DbContext"/> instance.  It is further extended to provide data synchronization
/// capabilities between the on-client database and a remote service.
/// </summary>
/// <remarks>
/// Each <see cref="DbSet{TEntity}"/> should be independent and not contain foreign relationships
/// so that the data synchronization process can push and pull tables independently.
/// </remarks>
public abstract partial class OfflineDbContext : DbContext
{
    /// <summary>
    /// The list of system table entities used by the OfflineDbContext that should not be configurable by the user for datasync services.
    /// </summary>
    private static readonly Type[] DatasyncSystemTables = [
        typeof(SynchronizationSetMetadata)
    ];

    /// <summary>
    /// The <see cref="DatasyncContext"/> contains the information on how to communicate with the
    /// datasync service and how to map entities to endpoints.
    /// </summary>
    private readonly Lazy<DatasyncContext> _datasyncContext;

    /// <summary>
    /// Initializes a new instance of the DbContext class. The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will be called to configure the database(and other options) to be used for this context.
    /// </summary>
    public OfflineDbContext() : base()
    {
        this._datasyncContext = new Lazy<DatasyncContext>(() => BuildDatasyncContext());
    }

    /// <summary>
    /// Initializes a new instance of the DbContext class using the specified options.The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will still be called to allow further configuration of the options.
    /// </summary>
    /// <param name="options">The options for this context</param>
    public OfflineDbContext(DbContextOptions options) : base(options)
    {
        this._datasyncContext = new Lazy<DatasyncContext>(() => BuildDatasyncContext());
    }

    /// <summary>
    /// The currently configure <see cref="DatasyncContext"/> for this database context.
    /// </summary>
    internal DatasyncContext DatasyncContext { get => this._datasyncContext.Value; }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Provides a hook that allows the developer to configure the DatasyncContext used in
    /// communicating with the datasync service.
    /// </summary>
    /// <param name="contextBuilder">The <see cref="DatasyncContextBuilder"/> that is being used to configure the service.</param>
    protected virtual void OnDatasyncInitialization(DatasyncContextBuilder contextBuilder)
    {
    }

    /// <summary>
    /// Builds the <see cref="DatasyncContext"/> for this set of offline tables.
    /// </summary>
    /// <returns>A <see cref="DatasyncContext"/> for the database context.</returns>
    internal DatasyncContext BuildDatasyncContext()
    {
        List<Type> entityTypes = Model.GetEntityTypes().Select(x => x.ClrType).Except(DatasyncSystemTables).ToList();
        DatasyncContextBuilder contextBuilder = new(entityTypes);
        OnDatasyncInitialization(contextBuilder);
        return contextBuilder.Build();
    }
}
