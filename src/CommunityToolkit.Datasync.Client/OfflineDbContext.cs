// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data.Common;

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
public abstract class OfflineDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the DbContext class. The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will be called to configure the database(and other options) to be used for this context.
    /// </summary>
    public OfflineDbContext() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the DbContext class using the specified options.The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will still be called to allow further configuration of the options.
    /// </summary>
    /// <param name="options">The options for this context</param>
    public OfflineDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// A <see cref="DbSet{TEntity}"/> for storing synchronization set metadata.  This is used by
    /// the <see cref="PullAsync"/> method to pull only the latest data for the table or query.
    /// </summary>
    protected DbSet<SynchronizationSetMetadata> SynchronizationSetMetadata => Set<SynchronizationSetMetadata>();

    /// <summary>
    /// A method used by synchronization methods to get a <see cref="HttpClient"/> that will be used
    /// to communicate with the remote service.
    /// </summary>
    /// <returns>A <see cref="HttpClient"/> that is configured for communicating with the remote service.</returns>
    protected abstract HttpClient GetHttpClient();

    /// <summary>
    /// The main pull operation for data synchronization.
    /// </summary>
    /// <param name="options">The options to use in this pull operations.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation when complete.</returns>
    public async ValueTask<PullResult> PullAsync(PullOperationOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
