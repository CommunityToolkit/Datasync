// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An <see cref="OfflineDbContext"/> instance represents a session with the database and can be used to query and save 
/// instances of your entities just like a <see cref="DbContext"/>.  It additionally supports synchronization handling to
/// a remote datasync service.
/// </summary>
public abstract class OfflineDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineDbContext"/> class. The <see cref="OfflineDbContext.OnConfiguring(DbContextOptionsBuilder)"/> 
    /// method will be called to configure the database (and other options) to be used for this context.
    /// </summary>
    protected OfflineDbContext() : base()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineDbContext"/> class using the specified options. The <see cref="OfflineDbContext.OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will still be called to allow further configuration of the options.
    /// </summary>
    /// <param name="contextOptions">The options for this context.</param>
    protected OfflineDbContext(DbContextOptions contextOptions) : base(contextOptions)
    {
    }

    /// <summary>
    /// Override this method to set defaults and configure conventions before they run. This method is invoked before
    /// <see cref="OfflineDbContext.OnModelCreating(ModelBuilder)"/>.
    /// </summary>
    /// <param name="configurationBuilder">The builder being used to set defaults and configure conventions that will be used to build the model for this context.</param>
    protected new virtual void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    /// <summary>
    /// Override this method to configure the database (and other options) to be used for this context. This method is called for 
    /// each instance of the context that is created. The base implementation does nothing.
    /// </summary>
    /// <param name="optionsBuilder">
    /// A builder used to create or modify options for this context. Databases (and other extensions) typically define extension 
    /// methods on this object that allow you to configure the context.
    /// </param>
    protected new virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Override this method to further configure the model that was discovered by convention from the entity types exposed in 
    /// <see cref="DbSet{TEntity}"/> properties on your derived context. The resulting model may be cached and re-used for subsequent
    /// instances of your derived context.
    /// </summary>
    /// <param name="modelBuilder">
    /// The builder being used to construct the model for this context. Databases (and other extensions) typically define extension
    /// methods on this object that allow you to configure aspects of the model that are specific to a given database.
    /// </param>
    protected new virtual void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Override this method to configure the settings on each entity that will be used to synchronize data to the service.
    /// </summary>
    /// <remarks>
    /// At a minimum, this method must be overridden to set the mechanism by which the <see cref="HttpClient"/> used to 
    /// communicate with the remote datasync service will be generated.  You can simply specify an endpoint, but it will
    /// be more normal to set up a <see cref="IHttpClientFactory"/> to generate clients for your service.
    /// </remarks>
    /// <param name="contextBuilder">The builder being used to construct the datasync synchronization context.</param>
    protected virtual void OnDatasyncInitializing(DatasyncSynchronizationContextBuilder contextBuilder)
    {
        if (!contextBuilder.HasHttpClientGenerator)
        {
            throw new DatasyncException(ServiceErrorMessages.NoHttpClientGenerator);
        }
    }
}
